﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            return RewriteConditionalAccess(node, used: true);
        }

        // null when currently enclosing conditional access node
        // is not supposed to be lowered.
        private BoundExpression _currentConditionalAccessTarget = null;
        private int _currentConditionalAccessID = 0;

        private enum ConditionalAccessLoweringKind
        {
            LoweredConditionalAccess,
            Ternary,
            TernaryCaptureReceiverByVal
        }

        // IL gen can generate more compact code for certain conditional accesses 
        // by utilizing stack dup/pop instructions 
        internal BoundExpression RewriteConditionalAccess(BoundConditionalAccess node, bool used, BoundExpression rewrittenWhenNull = null)
        {
            Debug.Assert(!_inExpressionLambda);

            var loweredReceiver = this.VisitExpression(node.Receiver);
            var receiverType = loweredReceiver.Type;

            ConditionalAccessLoweringKind loweringKind;
            // nullable and dynamic receivers are not directly supported in codegen and need to be lowered
            // in particular nullable receiver implies that the condition of the 
            // conditional and the access receiver are actually different expressions 
            // (HasValue and GetValueOrDefault respectively)
            var lowerToTernary = receiverType.IsNullableType() || node.AccessExpression.Type.IsDynamic();

            if (!lowerToTernary)
            {
                // trivial cases are directly supported in IL gen
                loweringKind = ConditionalAccessLoweringKind.LoweredConditionalAccess;
            }
            //       Nullable is special since we are not going to read any part of it twice
            //       we will read "HasValue" and then, conditionally will read "ValueOrDefault"
            else if (CanChangeValueBetweenReads(loweredReceiver, !receiverType.IsNullableType()))
            {
                // NOTE: dynamic operations historically do not propagate mutations
                // to the receiver if that hapens to be a value type
                // so we can capture receiver by value in dynamic case regardless of 
                // the type of receiver
                // Nullable receivers are immutable so should be captured by value as well.
                loweringKind = ConditionalAccessLoweringKind.TernaryCaptureReceiverByVal;
            }
            else
            {
                loweringKind = ConditionalAccessLoweringKind.Ternary;
            }


            var previousConditionalAccesTarget = _currentConditionalAccessTarget;
            var currentConditionalAccessID = ++this._currentConditionalAccessID;

            LocalSymbol temp = null;
            BoundExpression unconditionalAccess = null;

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.LoweredConditionalAccess:
                    _currentConditionalAccessTarget = new BoundConditionalReceiver(
                        loweredReceiver.Syntax, 
                        currentConditionalAccessID, 
                        receiverType);

                    break;

                case ConditionalAccessLoweringKind.Ternary:
                    _currentConditionalAccessTarget = loweredReceiver;
                    break;

                case ConditionalAccessLoweringKind.TernaryCaptureReceiverByVal:
                    temp = _factory.SynthesizedLocal(receiverType);
                    _currentConditionalAccessTarget = _factory.Local(temp);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweringKind);
            }

            BoundExpression loweredAccessExpression = used ?
                        this.VisitExpression(node.AccessExpression) :
                        this.VisitUnusedExpression(node.AccessExpression);

            _currentConditionalAccessTarget = previousConditionalAccesTarget;

            TypeSymbol type = this.VisitType(node.Type);

            TypeSymbol nodeType = node.Type;
            TypeSymbol accessExpressionType = loweredAccessExpression.Type;

            if (accessExpressionType.SpecialType == SpecialType.System_Void)
            {
                type = nodeType = accessExpressionType;
            }

            if (accessExpressionType != nodeType && nodeType.IsNullableType())
            {
                Debug.Assert(accessExpressionType == nodeType.GetNullableUnderlyingType());
                loweredAccessExpression = _factory.New((NamedTypeSymbol)nodeType, loweredAccessExpression);

                if (unconditionalAccess != null)
                {
                    unconditionalAccess = _factory.New((NamedTypeSymbol)nodeType, unconditionalAccess);
                }
            }
            else
            {
                Debug.Assert(accessExpressionType == nodeType ||
                    (nodeType.SpecialType == SpecialType.System_Void && !used));
            }

            BoundExpression result;
            var objectType = _compilation.GetSpecialType(SpecialType.System_Object);

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.LoweredConditionalAccess:
                    Debug.Assert(!receiverType.IsValueType);
                    result = new BoundLoweredConditionalAccess(
                        node.Syntax, 
                        loweredReceiver, 
                        loweredAccessExpression, 
                        rewrittenWhenNull,
                        currentConditionalAccessID,
                        type);

                    break;

                case ConditionalAccessLoweringKind.TernaryCaptureReceiverByVal:
                    // capture the receiver into a temp
                    loweredReceiver = _factory.Sequence(
                                            _factory.AssignmentExpression(_factory.Local(temp), loweredReceiver),
                                            _factory.Local(temp));

                    goto case ConditionalAccessLoweringKind.Ternary;

                case ConditionalAccessLoweringKind.Ternary:
                    {
                        // (object)r != null ? access : default(T)
                        var condition = receiverType.IsNullableType() ?
                            MakeOptimizedHasValue(loweredReceiver.Syntax, loweredReceiver) :
                            _factory.ObjectNotEqual(
                                _factory.Convert(objectType, loweredReceiver),
                                _factory.Null(objectType));

                        var consequence = loweredAccessExpression;

                        result = RewriteConditionalOperator(node.Syntax,
                            condition,
                            consequence,
                            rewrittenWhenNull ?? _factory.Default(nodeType),
                            null,
                            nodeType);

                        if (temp != null)
                        {
                            result = _factory.Sequence(temp, result);
                        }
                    }
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            return result;
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var newtarget = _currentConditionalAccessTarget;

            if (newtarget.Type.IsNullableType())
            {
                Debug.Assert(newtarget.Kind != BoundKind.ConditionalReceiver);
                newtarget = MakeOptimizedGetValueOrDefault(node.Syntax, newtarget);
            }

            return newtarget;
        }
    }
}
