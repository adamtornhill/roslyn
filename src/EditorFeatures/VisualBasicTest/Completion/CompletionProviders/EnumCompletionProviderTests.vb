' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class EnumCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        <Fact>
        <WorkItem(545678)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_EnumTypeDotMemberAlways()
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As MyEnum = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
Public Enum MyEnum
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)> Member
End Enum
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <Fact>
        <WorkItem(545678)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_EnumTypeDotMemberNever()
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As MyEnum = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
Public Enum MyEnum
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> Member
End Enum
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <Fact>
        <WorkItem(545678)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_EnumTypeDotMemberAdvanced()
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As MyEnum = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
Public Enum MyEnum
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)> Member
End Enum
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="MyEnum.Member",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Sub

        <Fact>
        <WorkItem(566787)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TriggeredOnOpenParen()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        ' type after this line
        Bar($$
    End Sub
 
    Sub Bar(f As Foo)
    End Sub
End Module
 
Enum Foo
    AMember
    BMember
    CMember
End
]]></Text>.Value

            VerifyItemExists(markup, "Foo.AMember", usePreviousCharAsTrigger:=True)
        End Sub

        <Fact>
        <WorkItem(674390)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RightSideOfAssignment()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x as Foo
        x = $$
    End Sub
End Module
 
Enum Foo
    AMember
    BMember
    CMember
End
]]></Text>.Value

            VerifyItemExists(markup, "Foo.AMember", usePreviousCharAsTrigger:=True)
        End Sub

        <Fact>
        <WorkItem(530491)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotCrashInObjectInitializer()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim z = New Foo() With {.z$$ }
    End Sub

    Class Foo
        Property A As Integer
            Get

            End Get
            Set(value As Integer)

            End Set
        End Property
    End Class
End Module
]]></Text>.Value

            VerifyNoItemsExist(markup)
        End Sub

        <Fact>
        <WorkItem(809332)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CaseStatement()
            Dim markup = <Text><![CDATA[
Enum E
    A
    B
    C
End Enum
 
Module Module1
    Sub Main(args As String())
        Dim value = E.A
 
        Select Case value
            Case $$
        End Select
 
    End Sub
End Module
]]></Text>.Value

            VerifyItemExists(markup, "E.A", usePreviousCharAsTrigger:=True)
        End Sub

        <Fact>
        <WorkItem(854099)>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInComment()
            Dim markup = <Text><![CDATA[
Enum E
    A
    B
    C
End Enum
 
Module Module1
    Sub Main(args As String())
        Dim value = E.A
 
        Select Case value
            Case E.A | $$
        End Select
 
    End Sub
End Module
]]></Text>.Value

            VerifyNoItemsExist(markup, usePreviousCharAsTrigger:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(827897)>
        Public Sub InYieldReturn()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

Class C
    Iterator Function M() As IEnumerable(Of DayOfWeek)
        Yield $$
    End Function
End Class
]]></Text>.Value

            VerifyItemExists(markup, "DayOfWeek.Friday")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(827897)>
        Public Sub InAsyncMethodReturnStatement()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Threading.Tasks

Class C
    Async Function M() As Task(Of DayOfWeek)
        Await Task.Delay(1)
        Return $$
    End Function
End Class
]]></Text>.Value

            VerifyItemExists(markup, "DayOfWeek.Friday")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(900625)>
        Public Sub InIndexedProperty()
            Dim markup = <Text><![CDATA[
Module Module1

    Enum MyEnum
        flower
    End Enum

    Public Class MyClass1
        Public WriteOnly Property MyProperty(ByVal val1 As MyEnum) As Boolean
            Set(ByVal value As Boolean)

            End Set
        End Property

        Public Sub MyMethod(ByVal val1 As MyEnum)

        End Sub
    End Class

    Sub Main()
        Dim var As MyClass1 = New MyClass1
        ' MARKER
        var.MyMethod(MyEnum.flower)
        var.MyProperty($$MyEnum.flower) = True
    End Sub

End Module
]]></Text>.Value

            VerifyItemExists(markup, "MyEnum.flower")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(916483)>
        Public Sub FullyQualified()
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As System.DayOfWeek)
        M($$)
    End Sub
 
    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            VerifyItemExists(markup, "System.DayOfWeek.Friday")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(916467)>
        Public Sub TriggeredForNamedArgument()
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        M(day:=$$)
    End Sub
 
    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            VerifyItemExists(markup, "DayOfWeek.A", usePreviousCharAsTrigger:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(916467)>
        Public Sub NotTriggeredAfterAssignmentEquals()
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Dim x = $$
    End Sub
 
    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            VerifyItemIsAbsent(markup, "DayOfWeek.A", usePreviousCharAsTrigger:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963)>
        Public Sub CaseStatementWithInt32InferredType()
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Select Case day
            Case DayOfWeek.A
            Case $$
        End Select
    End Sub

    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            VerifyItemExists(markup, "DayOfWeek.A")
            VerifyItemExists(markup, "DayOfWeek.B")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963)>
        Public Sub NotInTrivia()
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As DayOfWeek)
        Select Case day
            Case DayOfWeek.A,
                 DayOfWeek.B'$$
            Case
        End Select
    End Sub

    Enum DayOfWeek
        A
        B
    End Enum
End Class
]]></Text>.Value
            VerifyNoItemsExist(markup)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963)>
        Public Sub LocalNoAs()
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = e$$
    End Sub
End Class
]]></Text>.Value
            VerifyItemExists(markup, "e")
            VerifyItemIsAbsent(markup, "e As E")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963)>
        Public Sub IncludeEnumAfterTyping()
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = e$$
    End Sub
End Class
]]></Text>.Value
            VerifyItemExists(markup, "E")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(1015797)>
        Public Sub CommitOnComma()
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = $$
    End Sub
End Class
]]></Text>.Value

            Dim expected = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = E.A
    End Sub
End Class
]]></Text>.Value

            VerifyProviderCommit(markup, "E.A", expected, ","c, textTypedSoFar:="")
        End Sub

        Friend Overrides Function CreateCompletionProvider() As ICompletionProvider
            Return New EnumCompletionProvider()
        End Function
    End Class
End Namespace
