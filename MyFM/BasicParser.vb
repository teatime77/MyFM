Imports System.Diagnostics

Public MustInherit Class TSourceParser
    Public LanguageSP As ELanguage
    Public PrjParse As TProject
    Public vTknName As Dictionary(Of EToken, String)
    Public ThisName As String
    Public TranslationTable As New Dictionary(Of String, String)

    Public MustOverride Function Lex(src_text As String) As TList(Of TToken)
    Public Overridable Sub ReadAllStatement(src1 As TSourceFile)
    End Sub
    Public MustOverride Sub Parse(src1 As TSourceFile)
    Public MustOverride Sub ClearParse()
    Public MustOverride Sub RegAllClass(prj1 As TProject, src1 As TSourceFile)
    Public MustOverride Function NullName() As String


    Public Function TranslageReferenceName(ref1 As TReference) As String
        If TypeOf ref1.VarRef Is TField OrElse TypeOf ref1.VarRef Is TFunction Then

            Dim long_name As String = ref1.VarRef.GetClassVar().NameVar + "." + ref1.NameRef

            If TranslationTable.ContainsKey(long_name) Then
                Return TranslationTable(long_name)
            End If
        End If

        Return ref1.NameRef
    End Function
End Class

'-------------------------------------------------------------------------------- TBasicParser
' Basicの構文解析
Public Class TBasicParser
    Inherits TSourceParser


    Public vTkn As New Dictionary(Of String, EToken)
    Public CurBlc As TBlock
    Public CurPos As Integer
    Public CurTkn As TToken
    Public NxtTkn As TToken
    Dim EOTTkn As TToken
    Public CurVTkn As TList(Of TToken)
    Dim CurStmt As TStatement
    Public CurLineIdx As Integer
    Dim CurLineStr As String
    Dim ArgClassTable As Dictionary(Of String, TClass)

    Public Sub New(prj1 As TProject)
        LanguageSP = ELanguage.Basic
        ThisName = "Me"
        PrjParse = prj1
        RegTkn()
    End Sub

    Public Overrides Sub ClearParse()
        CurBlc = Nothing
        CurPos = 0
        CurTkn = Nothing
        NxtTkn = Nothing
        CurVTkn = Nothing
        CurStmt = Nothing
        CurLineIdx = 0
        CurLineStr = ""
    End Sub

    Public Overrides Function NullName() As String
        Return "Nothing"
    End Function

    Public Function GetTkn(type1 As EToken) As TToken
        Dim tkn1 As TToken

        If type1 = CurTkn.TypeTkn OrElse type1 = EToken.Unknown Then
            tkn1 = CurTkn
            CurPos = CurPos + 1
            If CurPos < CurVTkn.Count Then
                If CurVTkn(CurPos).TypeTkn = EToken.LowLine Then

                    CurLineIdx += 1
                    CurLineStr = PrjParse.CurSrc.vTextSrc(CurLineIdx)
                    CurVTkn = PrjParse.CurSrc.LineTkn(CurLineIdx)

                    CurPos = 0
                End If
                CurTkn = CurVTkn(CurPos)
                If CurPos + 1 < CurVTkn.Count Then
                    NxtTkn = CurVTkn(CurPos + 1)
                Else
                    NxtTkn = EOTTkn
                End If
            Else
                CurTkn = EOTTkn
                NxtTkn = EOTTkn
            End If

            Return tkn1
        Else
            Chk(False, CurLineStr)
            Return Nothing
        End If
    End Function

    Function ReadImports() As TStatement
        Dim stmt1 As New TImports
        Dim id1 As TToken
        Dim tkn1 As TToken
        Dim sb1 As TStringWriter

        stmt1.TypeStmt = EToken.Imports_
        GetTkn(EToken.Imports_)

        sb1 = New TStringWriter()
        Do While True
            id1 = GetTkn(EToken.Id)
            sb1.Append(id1.StrTkn)

            Select Case CurTkn.TypeTkn
                Case EToken.EOT
                    Exit Do
                Case EToken.Dot
                    tkn1 = GetTkn(EToken.Dot)
                    sb1.Append(tkn1.StrTkn)
                Case Else
                    Chk(False)
            End Select
        Loop

        PrjParse.CurSrc.vUsing.Add(sb1.ToString())

        Return stmt1
    End Function

    Function ReadModule() As TStatement
        Dim stmt1 As New TModule
        Dim id1 As TToken

        stmt1.TypeStmt = EToken.Module_
        GetTkn(EToken.Module_)
        id1 = GetTkn(EToken.Id)
        stmt1.NameMod = id1.StrTkn

        Return stmt1
    End Function

    Function ReadEnum() As TStatement
        Dim stmt1 As New TEnumStatement
        Dim id1 As TToken

        stmt1.TypeStmt = EToken.Enum_
        GetTkn(EToken.Enum_)
        id1 = GetTkn(EToken.Id)
        stmt1.NameEnumStmt = id1.StrTkn
        Return stmt1
    End Function

    Function ReadClass(mod1 As TModifier) As TStatement
        Dim stmt1 As New TClassStatement, cla1 As TClass
        Dim id1 As TToken

        PrjParse.dicGenCla.Clear()

        stmt1.TypeStmt = EToken.Class_
        Select Case CurTkn.TypeTkn
            Case EToken.Class_
                stmt1.KndClaStmt = EClass.eClassCla
            Case EToken.Struct
                stmt1.KndClaStmt = EClass.eStructCla
            Case EToken.Interface_
                stmt1.KndClaStmt = EClass.eInterfaceCla
        End Select
        GetTkn(EToken.Unknown)
        id1 = GetTkn(EToken.Id)
        cla1 = PrjParse.GetCla(id1.StrTkn)
        Debug.Assert(cla1 IsNot Nothing)
        cla1.ModVar = mod1
        stmt1.ClaClaStmt = cla1

        If CurTkn.TypeTkn = EToken.LP Then
            ' ジェネリック クラスの場合

            For Each cla2 In cla1.GenCla
                cla2.IsParamCla = True
                PrjParse.dicGenCla.Add(cla2.NameCla(), cla2)
            Next

            GetTkn(EToken.LP)
            GetTkn(EToken.Of_)

            Do While True
                GetTkn(EToken.Id)
                If CurTkn.TypeTkn = EToken.RP Then
                    GetTkn(EToken.RP)
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If

        Return stmt1
    End Function

    Function ReadInherits() As TStatement
        Dim stmt1 As New TInheritsStatement, id1 As TToken, id2 As TToken

        stmt1.TypeStmt = EToken.Extends
        GetTkn(EToken.Extends)
        id1 = GetTkn(EToken.Id)
        stmt1.ClassNameInheritsStmt = id1.StrTkn

        If CurTkn.TypeTkn = EToken.LP Then

            GetTkn(EToken.LP)
            GetTkn(EToken.Of_)

            stmt1.ParamName = New TList(Of String)()
            Do While True
                id2 = GetTkn(EToken.Id)
                stmt1.ParamName.Add(id2.StrTkn)

                If CurTkn.TypeTkn <> EToken.Comma Then

                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
            GetTkn(EToken.RP)

        End If
        Return stmt1
    End Function

    Function ReadImplements() As TStatement
        Dim stmt1 As New TImplementsStatement
        Dim cla1 As TClass

        stmt1.TypeStmt = EToken.Implements_
        GetTkn(EToken.Implements_)
        Do While True
            cla1 = ReadType(False)
            stmt1.ClassImplementsStmt.Add(cla1)

            If CurTkn.TypeTkn <> EToken.Comma Then
                Exit Do
            End If
            GetTkn(EToken.Comma)
        Loop
        Return stmt1
    End Function

    Function ReadSubFunction(mod1 As TModifier, is_delegate As Boolean) As TStatement
        Dim stmt1 As New TFunctionStatement
        Dim id1 As TToken, id2 As TToken, id3 As TToken
        Dim var1 As TVariable
        Dim by_ref As Boolean, param_array As Boolean
        Dim cla1 As TDelegate

        If is_delegate Then
            PrjParse.dicGenCla.Clear()
        End If

        stmt1.TypeStmt = CurTkn.TypeTkn
        stmt1.ModifierFncStmt = mod1
        stmt1.IsDelegateFncStmt = is_delegate
        GetTkn(EToken.Unknown)
        If CurTkn.TypeTkn = EToken.New_ Then
            GetTkn(EToken.New_)
            stmt1.TypeStmt = EToken.New_
        Else
            If stmt1.TypeStmt = EToken.Operator_ Then
                stmt1.OpFncStmt = CurTkn.TypeTkn
            Else
                Debug.Assert(CurTkn.TypeTkn = EToken.Id)
            End If
            id1 = GetTkn(EToken.Unknown)
            stmt1.NameFncStmt = id1.StrTkn
            If is_delegate Then
                cla1 = PrjParse.GetDelegate(stmt1.NameFncStmt)
                If CurTkn.TypeTkn = EToken.LP AndAlso NxtTkn.TypeTkn = EToken.Of_ Then

                    For Each cla_f In cla1.GenCla
                        PrjParse.dicGenCla.Add(cla_f.NameCla(), cla_f)
                    Next

                    GetTkn(EToken.LP)
                    GetTkn(EToken.Of_)
                    Do While True
                        GetTkn(EToken.Id)
                        If CurTkn.TypeTkn = EToken.RP Then
                            Exit Do
                        End If
                        GetTkn(EToken.Comma)
                    Loop
                    GetTkn(EToken.RP)
                End If
            End If
        End If

        If NxtTkn.TypeTkn = EToken.Of_ Then

            ArgClassTable = New Dictionary(Of String, TClass)()
            stmt1.ArgumentClassFncStmt = New TList(Of TClass)()

            GetTkn(EToken.LP)
            GetTkn(EToken.Of_)
            Do While True
                Dim class_name As TToken = GetTkn(EToken.Id)

                Dim arg_class = New TClass(PrjParse, class_name.StrTkn)
                arg_class.IsParamCla = True
                arg_class.GenericType = EGeneric.ArgumentClass

                ArgClassTable.Add(arg_class.NameVar, arg_class)
                stmt1.ArgumentClassFncStmt.Add(arg_class)

                If CurTkn.TypeTkn = EToken.RP Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
            GetTkn(EToken.RP)
        End If

        GetTkn(EToken.LP)
        If CurTkn.TypeTkn <> EToken.RP Then
            Do While True
                by_ref = False
                param_array = False
                Select Case CurTkn.TypeTkn
                    Case EToken.Ref
                        by_ref = True
                        GetTkn(EToken.Ref)
                    Case EToken.ParamArray_
                        param_array = True
                        GetTkn(EToken.ParamArray_)
                End Select
                var1 = ReadVariable(stmt1)
                var1.ByRefVar = by_ref
                var1.ParamArrayVar = param_array
                stmt1.ArgumentFncStmt.Add(var1)
                If CurTkn.TypeTkn <> EToken.Comma Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If
        GetTkn(EToken.RP)

        If stmt1.TypeStmt = EToken.Function_ OrElse stmt1.TypeStmt = EToken.Operator_ Then
            GetTkn(EToken.As_)
            stmt1.RetType = ReadType(False)
        End If

        If CurTkn.TypeTkn = EToken.Implements_ Then
            GetTkn(EToken.Implements_)

            id2 = GetTkn(EToken.Id)
            GetTkn(EToken.Dot)
            id3 = GetTkn(EToken.Id)

            stmt1.InterfaceFncStmt = PrjParse.GetCla(id2.StrTkn)
            Debug.Assert(stmt1.InterfaceFncStmt IsNot Nothing)
            stmt1.InterfaceFncName = id3.StrTkn
        End If

        If is_delegate Then
            PrjParse.dicGenCla.Clear()
        End If

        ArgClassTable = Nothing

        Return stmt1
    End Function

    ' ジェネリック型の構文解析
    Function ReadGenType(id1 As TToken) As TClass
        Dim tp1 As TClass, tp2 As TClass
        Dim vtp As TList(Of TClass)
        Dim is_param As Boolean = False

        GetTkn(EToken.LP)
        GetTkn(EToken.Of_)

        vtp = New TList(Of TClass)()
        Do While True
            tp2 = ReadType(False)
            If tp2.GenericType = EGeneric.ArgumentClass Then
                is_param = True
            End If
            vtp.Add(tp2)

            If CurTkn.TypeTkn <> EToken.Comma Then

                Exit Do
            End If
            GetTkn(EToken.Comma)
        Loop
        GetTkn(EToken.RP)

        ' ジェネリック型のクラスを得る。
        tp1 = PrjParse.GetAddSpecializedClass(id1.StrTkn, vtp)
        tp1.ContainsArgumentClass = is_param

        Return tp1
    End Function

    Function ReadType(is_new As Boolean) As TClass
        Dim tp1 As TClass
        Dim id1 As TToken, dim_cnt As Integer

        id1 = GetTkn(EToken.Id)
        If CurTkn.TypeTkn = EToken.LP AndAlso NxtTkn.TypeTkn = EToken.Of_ Then
            ' ジェネリック型の場合

            ' ジェネリック型の構文解析
            tp1 = ReadGenType(id1)
        Else

            tp1 = PrjParse.GetCla(id1.StrTkn)
            If tp1 Is Nothing Then
                If ArgClassTable IsNot Nothing Then
                    If ArgClassTable.ContainsKey(id1.StrTkn) Then
                        tp1 = ArgClassTable(id1.StrTkn)
                    End If
                End If
                If tp1 Is Nothing Then
                    Throw New TError(String.Format("不明なクラス {0}", id1.StrTkn))
                End If
            End If
        End If
        If CurTkn.TypeTkn = EToken.LP AndAlso (NxtTkn.TypeTkn = EToken.RP OrElse NxtTkn.TypeTkn = EToken.Comma) Then
            GetTkn(EToken.LP)
            dim_cnt = 1
            Do While CurTkn.TypeTkn = EToken.Comma
                GetTkn(EToken.Comma)
                dim_cnt += 1
            Loop
            GetTkn(EToken.RP)
            If Not is_new Then
                tp1 = PrjParse.GetArrCla(tp1, dim_cnt)
            End If
        End If

        Return tp1
    End Function

    Function ReadVariable(up1 As Object) As TVariable
        Dim var1 As New TLocalVariable
        Dim id1 As TToken
        Dim app1 As TApply

        id1 = GetTkn(EToken.Id)
        var1.NameVar = id1.StrTkn

        If CurTkn.TypeTkn = EToken.As_ Then

            GetTkn(EToken.As_)

            If CurTkn.TypeTkn = EToken.New_ Then
                app1 = NewExpression()
                var1.TypeVar = app1.NewApp
                var1.InitVar = app1

                Return var1
            End If

            var1.TypeVar = ReadType(False)
        End If

        If CurTkn.TypeTkn = EToken.Eq Then
            GetTkn(EToken.Eq)

            var1.InitVar = AdditiveExpression()
        End If

        Return var1
    End Function

    Function ReadTailCom() As String
        Dim tkn1 As TToken

        If CurTkn Is EOTTkn Then
            Return ""
        Else
            tkn1 = GetTkn(EToken.LineComment)
            Return tkn1.StrTkn
        End If
    End Function

    Function ReadDim(mod1 As TModifier) As TStatement
        Dim stmt1 As New TVariableDeclaration
        Dim var1 As TVariable

        stmt1.TypeStmt = EToken.VarDecl
        stmt1.ModDecl = mod1
        Do While True
            var1 = ReadVariable(stmt1)
            stmt1.VarDecl.Add(var1)
            If CurTkn.TypeTkn <> EToken.Comma Then
                Exit Do
            End If
            GetTkn(EToken.Comma)
        Loop

        stmt1.TailCom = ReadTailCom()

        Return stmt1
    End Function

    Function ReadReturn(type_tkn As EToken) As TReturn
        GetTkn(type_tkn)
        If CurTkn Is EOTTkn Then

            Return New TReturn(Nothing, type_tkn = EToken.Yield_)
        End If

        Return New TReturn(TermExpression(), type_tkn = EToken.Yield_)
    End Function

    Function ReadEnd() As TStatement
        Dim stmt1 As New TStatement

        GetTkn(EToken.End_)
        Select Case CurTkn.TypeTkn
            Case EToken.If_
                stmt1.TypeStmt = EToken.EndIf_
            Case EToken.Sub_
                stmt1.TypeStmt = EToken.EndSub
            Case EToken.Function_
                stmt1.TypeStmt = EToken.EndFunction
            Case EToken.Operator_
                stmt1.TypeStmt = EToken.EndOperator
            Case EToken.Class_
                PrjParse.dicGenCla.Clear()
                stmt1.TypeStmt = EToken.EndClass
            Case EToken.Struct
                stmt1.TypeStmt = EToken.EndStruct
            Case EToken.Interface_
                stmt1.TypeStmt = EToken.EndInterface
            Case EToken.Enum_
                stmt1.TypeStmt = EToken.EndEnum
            Case EToken.Module_
                stmt1.TypeStmt = EToken.EndModule
            Case EToken.Select_
                stmt1.TypeStmt = EToken.EndSelect
            Case EToken.Try_
                stmt1.TypeStmt = EToken.EndTry
            Case EToken.With_
                stmt1.TypeStmt = EToken.EndWith
            Case Else
                Chk(False)
        End Select
        GetTkn(EToken.Unknown)

        Return stmt1
    End Function

    Function ReadIf() As TStatement
        Dim stmt1 As New TIfStatement

        stmt1.TypeStmt = EToken.If_
        GetTkn(EToken.If_)
        stmt1.CndIfStmt = CType(TermExpression(), TTerm)
        GetTkn(EToken.Then_)
        Return stmt1
    End Function

    Function ReadElseIf() As TStatement
        Dim stmt1 As New TElseIf

        stmt1.TypeStmt = EToken.ElseIf_
        GetTkn(EToken.ElseIf_)
        stmt1.CndElseIf = CType(TermExpression(), TTerm)
        GetTkn(EToken.Then_)
        Return stmt1
    End Function

    Function ReadElse() As TStatement
        Dim stmt1 As New TStatement

        stmt1.TypeStmt = EToken.Else_
        GetTkn(EToken.Else_)
        Return stmt1
    End Function

    Function ReadDo() As TStatement
        Dim stmt1 As New TDoStmt

        stmt1.TypeStmt = EToken.Do_
        GetTkn(EToken.Do_)
        GetTkn(EToken.While_)
        stmt1.CndDo = CType(TermExpression(), TTerm)
        Return stmt1
    End Function

    Function ReadLoop() As TStatement
        Dim stmt1 As New TStatement

        stmt1.TypeStmt = EToken.Loop_
        GetTkn(EToken.Loop_)

        Return stmt1
    End Function

    Function ReadSelect() As TStatement
        Dim stmt1 As New TSelectStatement

        stmt1.TypeStmt = EToken.Switch
        GetTkn(EToken.Select_)
        GetTkn(EToken.Case_)
        stmt1.TermSelectStatement = CType(TermExpression(), TTerm)
        Return stmt1
    End Function

    Function ReadCase() As TStatement
        Dim stmt1 As New TCaseStatement
        Dim trm1 As TTerm

        stmt1.TypeStmt = EToken.Case_
        GetTkn(EToken.Case_)

        If CurTkn.TypeTkn = EToken.Else_ Then
            GetTkn(EToken.Else_)
            stmt1.IsCaseElse = True
        Else
            Do While True
                trm1 = CType(TermExpression(), TTerm)
                stmt1.TermCaseStmt.Add(trm1)
                If CurTkn.TypeTkn <> EToken.Comma Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If

        Return stmt1
    End Function

    Function ReadFor() As TStatement
        Dim stmt1 As New TForStatement, id1 As TToken

        stmt1.TypeStmt = EToken.For_
        GetTkn(EToken.For_)

        If CurTkn.TypeTkn = EToken.Id Then

            id1 = GetTkn(EToken.Id)
            stmt1.IdxForStmt = New TReference(id1)
            GetTkn(EToken.Eq)
            stmt1.FromForStmt = CType(TermExpression(), TTerm)
            GetTkn(EToken.To_)
            stmt1.ToForStmt = CType(TermExpression(), TTerm)

            If CurTkn.TypeTkn = EToken.Step_ Then
                GetTkn(EToken.Step_)
                stmt1.StepForStmt = CType(TermExpression(), TTerm)
            End If
        Else

            GetTkn(EToken.Each_)

            If CurTkn.TypeTkn = EToken.Id Then

                id1 = GetTkn(EToken.Id)
                stmt1.InVarForStmt = New TLocalVariable(id1.StrTkn, Nothing)
            End If

            If CurTkn.TypeTkn = EToken.At_ Then

                GetTkn(EToken.At_)

                Do While True
                    GetTkn(EToken.Id)
                    If CurTkn.TypeTkn <> EToken.Comma Then
                        Exit Do
                    End If
                    GetTkn(EToken.Comma)
                Loop
            End If

            GetTkn(EToken.In_)

            stmt1.InTrmForStmt = CType(TermExpression(), TTerm)
        End If

        Return stmt1
    End Function

    Function ReadNext() As TStatement
        Dim stmt1 As New TStatement

        stmt1.TypeStmt = EToken.Next_
        GetTkn(EToken.Next_)

        Return stmt1
    End Function

    Function ReadExit() As TStatement
        Dim stmt1 As New TExit

        GetTkn(EToken.Exit_)
        Select Case CurTkn.TypeTkn
            Case EToken.Do_
                stmt1.TypeStmt = EToken.ExitDo
            Case EToken.For_
                stmt1.TypeStmt = EToken.ExitFor
            Case EToken.Sub_
                stmt1.TypeStmt = EToken.ExitSub
            Case Else
                Chk(False)
        End Select
        GetTkn(EToken.Unknown)

        Return stmt1
    End Function

    Function ReadTry() As TStatement
        Dim stmt1 As New TStatement

        stmt1.TypeStmt = EToken.Try_
        GetTkn(EToken.Try_)
        Return stmt1
    End Function

    Function ReadCatch() As TStatement
        Dim stmt1 As New TCatchStatement

        stmt1.TypeStmt = EToken.Catch_
        GetTkn(EToken.Catch_)
        stmt1.VariableCatchStmt = ReadVariable(stmt1)
        Return stmt1
    End Function

    Function ReadWith() As TStatement
        Dim stmt1 As New TWithStmt

        stmt1.TypeStmt = EToken.With_
        GetTkn(EToken.With_)

        stmt1.TermWith = DotExpression()

        Return stmt1
    End Function

    Function ReadThrow() As TStatement
        Dim stmt1 As TThrow

        GetTkn(EToken.Throw_)
        stmt1 = New TThrow(CType(TermExpression(), TTerm))

        Return stmt1
    End Function

    Function ReadReDim() As TStatement
        Dim stmt1 As TReDim, trm1 As TTerm, app1 As TApply

        GetTkn(EToken.ReDim_)

        trm1 = TermExpression()
        Debug.Assert(trm1.IsApp())
        app1 = CType(trm1, TApply)
        Debug.Assert(app1.FncApp IsNot Nothing AndAlso app1.ArgApp.Count <> 0)
        stmt1 = New TReDim(app1.FncApp, app1.ArgApp)

        Return stmt1
    End Function

    Function ReadLineComment() As TStatement
        Dim stmt1 As New TStatement

        stmt1.TypeStmt = EToken.LineComment
        GetTkn(EToken.LineComment)
        Return stmt1
    End Function

    Sub Chk(b1 As Boolean, msg As String)
        If Not b1 Then
            Debug.WriteLine("コンパイラ　エラー {0} at {1}", CurLineStr, CurTkn.StrTkn)
            Debug.Assert(False)
        End If
    End Sub

    Sub Chk(b1 As Boolean)
        Chk(b1, "")
    End Sub

    Sub MakeModule(src1 As TSourceFile)
        Dim fnc1 As TFunction, is_module As Boolean = False, cla1 As TClassStatement, cla2 As TClass, com1 As TComment = Nothing

        CurLineIdx = 0
        Do While True
            CurStmt = GetNextStatement()
            If CurStmt Is Nothing OrElse CurStmt.TypeStmt <> EToken.Imports_ Then
                Exit Do
            End If
        Loop

        is_module = (CurStmt.TypeStmt = EToken.Module_)
        If is_module Then
            GetStatement(EToken.Module_)
        End If
        Do While CurStmt IsNot Nothing AndAlso CurStmt.TypeStmt <> EToken.EndModule
            Select Case CurStmt.TypeStmt
                Case EToken.Class_
                    cla1 = CType(CurStmt, TClassStatement)
                    cla2 = MakeClass()
                    cla2.ComVar = com1
                    cla2.SrcCla = PrjParse.CurSrc
                    com1 = Nothing
                Case EToken.Enum_
                    cla2 = MakeEnum()
                    cla2.ComVar = com1
                    cla2.SrcCla = PrjParse.CurSrc
                    com1 = Nothing
                Case EToken.Sub_, EToken.Function_
                    If CType(CurStmt, TFunctionStatement).IsDelegateFncStmt Then
                        cla2 = MakeDelegate()
                        cla2.ComVar = com1
                        cla2.SrcCla = PrjParse.CurSrc
                    Else
                        fnc1 = MakeSubFnc(Nothing)
                        fnc1.ComVar = com1
                    End If
                    com1 = Nothing
                Case EToken.Comment
                    com1 = CType(CurStmt, TComment)
                    GetStatement(EToken.Comment)
                Case Else
                    Chk(False)
            End Select
        Loop

        If is_module Then
            GetStatement(EToken.EndModule)
        End If
    End Sub

    Public Function MakeEnum() As TClass
        Dim cla1 As TClass
        Dim fld1 As TField
        Dim enum1 As TEnumStatement
        Dim ele1 As TEnumElement
        Dim type1 As TClass

        enum1 = CType(GetStatement(EToken.Enum_), TEnumStatement)
        cla1 = PrjParse.GetCla(enum1.NameEnumStmt)
        Debug.Assert(cla1 IsNot Nothing)
        PrjParse.CurSrc.ClaSrc.Add(cla1)

        cla1.KndCla = EClass.eEnumCla
        cla1.SuperClassList.Add(PrjParse.ObjectType)
        type1 = cla1

        Do While CurStmt.TypeStmt <> EToken.EndEnum
            ele1 = CType(GetStatement(EToken.Id), TEnumElement)
            fld1 = New TField(ele1.NameEnumEle, type1)
            cla1.AddFld(fld1)
        Loop
        GetStatement(EToken.EndEnum)
        cla1.Parsed = True

        Return cla1
    End Function

    Public Function MakeClass() As TClass
        Dim cla1 As TClass, spr_cla As TClass, vtp As TList(Of TClass)
        Dim fld1 As TField
        Dim fnc1 As TFunction
        Dim cla_stmt As TClassStatement
        Dim var_decl As TVariableDeclaration
        Dim instmt As TInheritsStatement, implstmt As TImplementsStatement, com1 As TComment = Nothing

        cla_stmt = CType(CurStmt, TClassStatement)
        cla1 = cla_stmt.ClaClaStmt

        cla1.KndCla = cla_stmt.KndClaStmt
        PrjParse.CurSrc.ClaSrc.Add(cla1)

        If cla1.GenCla IsNot Nothing Then
            ' ジェネリック クラスの場合

            ' for Add
            For Each cla_f In cla1.GenCla
                cla_f.IsParamCla = True
                PrjParse.dicGenCla.Add(cla_f.NameCla(), cla_f)
            Next
        End If

        GetStatement(EToken.Class_)

        If CurStmt.TypeStmt = EToken.Extends Then
            instmt = CType(GetStatement(EToken.Extends), TInheritsStatement)
            If instmt.ParamName Is Nothing Then
                spr_cla = PrjParse.GetCla(instmt.ClassNameInheritsStmt)
            Else
                vtp = New TList(Of TClass)()
                For Each s In instmt.ParamName
                    vtp.Add(PrjParse.GetCla(s))
                Next
                spr_cla = PrjParse.GetAddSpecializedClass(instmt.ClassNameInheritsStmt, vtp)
            End If
            cla1.SuperClassList.Add(spr_cla)

        End If

        If CurStmt.TypeStmt = EToken.Implements_ Then
            implstmt = CType(GetStatement(EToken.Implements_), TImplementsStatement)

            cla1.InterfaceList = implstmt.ClassImplementsStmt
        End If

        If PrjParse.ObjectType Is Nothing Then
            Debug.Assert(cla1.NameCla() = "Object")
            PrjParse.ObjectType = cla1
        End If
        Select Case cla1.NameCla()
            Case "System"
                PrjParse.SystemType = cla1
            Case "String"
                PrjParse.StringType = cla1
            Case "Char"
                PrjParse.CharType = cla1
            Case "Integer"
                PrjParse.IntType = cla1
            Case "Double"
                PrjParse.DoubleType = cla1
            Case "Type"
                PrjParse.TypeType = cla1
            Case "Boolean"
                PrjParse.BoolType = cla1
            Case "WaitHandle"
                PrjParse.WaitHandleType = cla1
        End Select

        If PrjParse.ObjectType IsNot cla1 AndAlso cla1.SuperClassList.Count = 0 Then
            cla1.SuperClassList.Add(PrjParse.ObjectType)
        End If

        Do While CurStmt.TypeStmt <> EToken.EndClass AndAlso CurStmt.TypeStmt <> EToken.EndStruct AndAlso CurStmt.TypeStmt <> EToken.EndInterface
            If CurStmt.TypeStmt = EToken.VarDecl Then
                var_decl = CType(GetStatement(EToken.VarDecl), TVariableDeclaration)
                ' for Add
                For Each var_f In var_decl.VarDecl
                    fld1 = New TField(var_f.NameVar, var_f.TypeVar, var_f.InitVar)
                    fld1.ComVar = com1
                    com1 = Nothing
                    fld1.ModVar = var_decl.ModDecl
                    fld1.TailCom = var_decl.TailCom
                    cla1.AddFld(fld1)
                Next
            ElseIf CurStmt.TypeStmt = EToken.Comment Then
                com1 = CType(GetStatement(EToken.Comment), TComment)
            Else
                fnc1 = MakeSubFnc(cla1)
                fnc1.ComVar = com1
                com1 = Nothing
                fnc1.ClaFnc = cla1
                cla1.FncCla.Add(fnc1)
            End If
        Loop
        GetStatement(EToken.Unknown)

        PrjParse.dicGenCla.Clear()
        cla1.Parsed = True

        Return cla1
    End Function

    '  ブロックの構文解析をする
    Function BlcParse(up1 As Object) As TBlock
        Dim blc1 As TBlock
        Dim blc_sv As TBlock
        Dim var_decl As TVariableDeclaration
        Dim if1 As TIfStatement
        Dim if2 As TIf
        Dim for1 As TForStatement
        Dim for2 As TFor
        Dim do1 As TDoStmt
        Dim sel1 As TSelectStatement
        Dim sel2 As TSelect
        Dim case1 As TCaseStatement
        Dim case2 As TCase
        Dim eif1 As TElseIf
        Dim try1 As TTry
        Dim catch1 As TCatchStatement
        Dim with1 As TWithStmt, with2 As TWith
        Dim if_blc As TIfBlock

        blc1 = New TBlock()

        blc_sv = CurBlc
        CurBlc = blc1

        Do While True
            Select Case CurStmt.TypeStmt
                Case EToken.ASN, EToken.Call_, EToken.Return_, EToken.Throw_, EToken.ExitDo, EToken.ExitFor, EToken.ExitSub, EToken.Goto_, EToken.Label, EToken.Comment, EToken.ReDim_
                    CurBlc.AddStmtBlc(CurStmt)
                    GetStatement(EToken.Unknown)
                Case EToken.If_
                    if1 = CType(GetStatement(EToken.If_), TIfStatement)
                    if2 = New TIf()
                    CurBlc.AddStmtBlc(if2)
                    if_blc = New TIfBlock(if1.CndIfStmt, BlcParse(if2))
                    if2.IfBlc.Add(if_blc)
                    Do While True
                        Select Case CurStmt.TypeStmt
                            Case EToken.ElseIf_
                                eif1 = CType(GetStatement(EToken.ElseIf_), TElseIf)
                                if_blc = New TIfBlock(eif1.CndElseIf, BlcParse(if2))
                                if2.IfBlc.Add(if_blc)
                            Case EToken.Else_
                                GetStatement(EToken.Else_)
                                if_blc = New TIfBlock(Nothing, BlcParse(if2))
                                if2.IfBlc.Add(if_blc)
                                GetStatement(EToken.EndIf_)
                                Exit Do
                            Case Else
                                GetStatement(EToken.EndIf_)
                                Exit Do
                        End Select
                    Loop

                Case EToken.For_
                    for1 = CType(GetStatement(EToken.For_), TForStatement)
                    for2 = New TFor()

                    for2.IdxFor = for1.IdxForStmt
                    for2.FromFor = for1.FromForStmt
                    for2.ToFor = for1.ToForStmt
                    for2.StepFor = for1.StepForStmt
                    for2.InVarFor = for1.InVarForStmt
                    for2.InTrmFor = for1.InTrmForStmt

                    for2.BlcFor = BlcParse(for2)
                    CurBlc.AddStmtBlc(for2)
                    GetStatement(EToken.Next_)

                Case EToken.Do_
                    do1 = CType(GetStatement(EToken.Do_), TDoStmt)
                    for2 = New TFor()
                    for2.IsDo = True
                    CurBlc.AddStmtBlc(for2)
                    for2.CndFor = do1.CndDo
                    for2.BlcFor = BlcParse(for2)
                    GetStatement(EToken.Loop_)

                Case EToken.Switch
                    sel1 = CType(GetStatement(EToken.Switch), TSelectStatement)
                    sel2 = New TSelect()
                    sel2.TrmSel = sel1.TermSelectStatement
                    CurBlc.AddStmtBlc(sel2)
                    Do While CurStmt.TypeStmt <> EToken.EndSelect
                        case1 = CType(GetStatement(EToken.Case_), TCaseStatement)
                        case2 = New TCase()
                        case2.TrmCase = case1.TermCaseStmt
                        case2.DefaultCase = case1.IsCaseElse
                        sel2.CaseSel.Add(case2)
                        case2.BlcCase = BlcParse(sel1)
                    Loop
                    GetStatement(EToken.EndSelect)

                Case EToken.Try_
                    GetStatement(EToken.Try_)
                    try1 = New TTry()
                    CurBlc.AddStmtBlc(try1)
                    try1.BlcTry = BlcParse(try1)
                    catch1 = CType(GetStatement(EToken.Catch_), TCatchStatement)
                    try1.VarCatch = New TList(Of TVariable)()
                    try1.VarCatch.Add(catch1.VariableCatchStmt)
                    try1.BlcCatch = BlcParse(try1)
                    GetStatement(EToken.EndTry)

                Case EToken.With_
                    with1 = CType(GetStatement(EToken.With_), TWithStmt)
                    with2 = New TWith()
                    with2.TermWith = with1.TermWith
                    CurBlc.AddStmtBlc(with2)
                    with2.BlcWith = BlcParse(with2)
                    GetStatement(EToken.EndWith)

                Case EToken.VarDecl
                    var_decl = CType(GetStatement(EToken.VarDecl), TVariableDeclaration)
                    CurBlc.AddStmtBlc(var_decl)
                    ' for Add
                    For Each var1 In var_decl.VarDecl
                        CurBlc.VarBlc.Add(var1)
                    Next

                Case EToken.Exit_
                    Chk(False)
                Case Else
                    Exit Do
            End Select
        Loop

        CurBlc = blc_sv

        Return blc1
    End Function

    Public Function MakeDelegate() As TClass
        Dim stmt1 As TFunctionStatement
        Dim dlg1 As TDelegate
        Dim fnc1 As TFunction

        stmt1 = CType(GetStatement(EToken.Unknown), TFunctionStatement)
        dlg1 = PrjParse.GetDelegate(stmt1.NameFncStmt)
        dlg1.Parsed = True

        dlg1.KndCla = EClass.eDelegateCla
        dlg1.RetDlg = stmt1.RetType
        dlg1.ArgDlg = stmt1.ArgumentFncStmt
        fnc1 = New TFunction("Invoke", stmt1.RetType)
        fnc1.SetModFnc(stmt1.ModifierFncStmt)
        fnc1.ArgFnc = stmt1.ArgumentFncStmt
        fnc1.ThisFnc = New TLocalVariable(ThisName, dlg1)
        fnc1.ClaFnc = dlg1
        dlg1.FncCla.Add(fnc1)

        PrjParse.CurSrc.ClaSrc.Add(dlg1)

        Return dlg1
    End Function

    Public Function MakeSubFnc(cla1 As TClass) As TFunction
        Dim fnc1 As TFunctionStatement
        Dim fnc2 As TFunction, fnc_name As String

        Chk(CurStmt.TypeStmt = EToken.Sub_ OrElse CurStmt.TypeStmt = EToken.Function_ OrElse CurStmt.TypeStmt = EToken.New_ OrElse CurStmt.TypeStmt = EToken.Operator_)
        fnc1 = CType(GetStatement(EToken.Unknown), TFunctionStatement)
        If fnc1.TypeStmt = EToken.New_ Then
            fnc_name = "New@" + cla1.NameCla()
        Else
            fnc_name = fnc1.NameFncStmt
        End If
        fnc2 = New TFunction(fnc_name, fnc1.RetType)
        fnc2.SetModFnc(fnc1.ModifierFncStmt)
        fnc2.TypeFnc = fnc1.TypeStmt
        fnc2.OpFnc = fnc1.OpFncStmt
        fnc2.ArgumentClassFnc = fnc1.ArgumentClassFncStmt
        fnc2.ArgFnc.AddRange(fnc1.ArgumentFncStmt)
        fnc2.ThisFnc = New TLocalVariable(ThisName, cla1)
        fnc2.InterfaceFnc = fnc1.InterfaceFncStmt
        fnc2.ImplFnc = New TReference(fnc1.InterfaceFncName)
        fnc2.IsNew = (fnc1.TypeStmt = EToken.New_)

        If fnc2.ModFnc().isMustOverride Then
            Return fnc2
        End If

        If cla1 Is Nothing OrElse cla1.KndCla <> EClass.eInterfaceCla Then
            ' インターフェイスでない場合

            Dim blc1 As TBlock = BlcParse(fnc2)
            If blc1.StmtBlc.Count = 1 AndAlso TypeOf blc1.StmtBlc(0) Is TWith Then
                Dim with1 As TWith = CType(blc1.StmtBlc(0), TWith)
                Dim ref1 As TReference = CType(with1.TermWith, TReference)
                Debug.Assert(ref1.NameRef = fnc2.ArgFnc(0).NameVar)
                If ref1.CastType IsNot Nothing Then
                    fnc2.WithFnc = ref1.CastType
                Else
                    fnc2.WithFnc = fnc2.ArgFnc(0).TypeVar
                End If

                fnc2.BlcFnc = with1.BlcWith
            Else
                fnc2.BlcFnc = blc1
            End If
            Chk(CurStmt.TypeStmt = EToken.EndSub OrElse CurStmt.TypeStmt = EToken.EndFunction OrElse CurStmt.TypeStmt = EToken.EndOperator)
            GetStatement(EToken.Unknown)
        End If

        Return fnc2
    End Function

    Public Overrides Sub ReadAllStatement(src1 As TSourceFile)
        Dim i1 As Integer, is_err As Boolean = False
        Dim com1 As TComment, stmt1 As TStatement, cla1 As TClass

        ' 文の配列を初期化する
        src1.StmtSrc = New TList(Of TStatement)()
        ' for Add
        For i1 = 0 To src1.vTextSrc.Length - 1
            src1.StmtSrc.Add(Nothing)
        Next

        com1 = New TComment()
        ' for ???
        i1 = 0
        Do While i1 < src1.vTextSrc.Length
            CurLineIdx = i1

            CurLineStr = src1.vTextSrc(i1)
            CurVTkn = src1.LineTkn(i1)

            If CurVTkn.Count = 0 Then
                '  空行の場合

                com1.LineCom.Add("")
            ElseIf CurVTkn(0).TypeTkn = EToken.LineComment Then
                '  コメントの場合

                com1.LineCom.Add(CurVTkn(0).StrTkn)
            Else
                '  空行やコメントでない場合

                If com1.LineCom.Count <> 0 Then
                    ' コメント・空行がある場合

                    ' 前の行にコメント文を入れる
                    src1.StmtSrc(i1 - 1) = com1

                    com1 = New TComment()
                End If

                Try
                    stmt1 = ReadStatement()

                    src1.StmtSrc(i1) = stmt1

                    ' 継続行の場合
                    Dim i2 As Integer = i1
                    Do While i2 < CurLineIdx
                        i2 = i2 + 1
                        src1.StmtSrc(i2) = Nothing
                    Loop

                    If TypeOf stmt1 Is TClassStatement Then
                        ' クラス定義の始まりの場合

                        cla1 = CType(stmt1, TClassStatement).ClaClaStmt
                        PrjParse.dicGenCla.Clear()
                        If cla1.GenCla IsNot Nothing Then
                            ' ジェネリック クラスの場合

                            For Each cla_f In cla1.GenCla
                                cla_f.IsParamCla = True
                                PrjParse.dicGenCla.Add(cla_f.NameCla(), cla_f)
                            Next
                        End If

                    ElseIf stmt1.TypeStmt = EToken.EndClass Then
                        ' クラス定義の終わりの場合

                        PrjParse.dicGenCla.Clear()
                    End If
                Catch ex As TError
                    is_err = True
                End Try
            End If

            i1 = CurLineIdx + 1
        Loop

        Debug.Assert(Not is_err)
    End Sub

    Public Function ReadModifier() As TModifier
        Dim mod1 As TModifier

        mod1 = New TModifier()
        mod1.ValidMod = False
        Do While True
            Select Case CurTkn.TypeTkn
                Case EToken.Partial_
                    mod1.isPartial = True
                Case EToken.Public_
                    mod1.isPublic = True
                Case EToken.Shared_
                    mod1.isShared = True
                Case EToken.Const_
                    mod1.isConst = True
                Case EToken.Abstract
                    mod1.isAbstract = True
                Case EToken.Virtual
                    mod1.isVirtual = True
                Case EToken.MustOverride_
                    mod1.isMustOverride = True
                Case EToken.Override
                    mod1.isOverride = True
                Case EToken.Iterator_
                    mod1.isIterator = True
                Case EToken.Protected_, EToken.Friend_, EToken.Private_

                Case EToken.LT
                    GetTkn(EToken.LT)
                    Do While True
                        Dim id1 As TToken

                        id1 = GetTkn(EToken.Id)
                        If id1.StrTkn = "XmlIgnoreAttribute" Then
                            mod1.isXmlIgnore = True
                        ElseIf id1.StrTkn = "_Weak" Then
                            mod1.isWeak = True
                        ElseIf id1.StrTkn = "_Parent" Then
                            mod1.isParent = True
                        ElseIf id1.StrTkn = "_Prev" Then
                            mod1.isPrev = True
                        ElseIf id1.StrTkn = "_Next" Then
                            mod1.isNext = True
                        ElseIf id1.StrTkn = "_Invariant" Then
                            mod1.isInvariant = True
                        Else
                            Debug.Assert(False)
                        End If
                        GetTkn(EToken.LP)
                        GetTkn(EToken.RP)

                        If CurTkn.TypeTkn <> EToken.Comma Then
                            Exit Do
                        End If
                        GetTkn(EToken.Comma)

                    Loop
                    Debug.Assert(CurTkn.TypeTkn = EToken.GT)

                Case Else
                    Exit Do
            End Select
            GetTkn(EToken.Unknown)
            mod1.ValidMod = True
        Loop

        Return mod1
    End Function

    Function ReadStatement() As TStatement
        Dim mod1 As TModifier, stmt1 As TStatement


        CurPos = 0
        CurTkn = CurVTkn(0)
        If 1 < CurVTkn.Count Then
            NxtTkn = CurVTkn(1)
        Else
            NxtTkn = EOTTkn
        End If
        stmt1 = Nothing

        '  修飾子を調べる

        mod1 = ReadModifier()

        If mod1.ValidMod AndAlso CurTkn.TypeTkn = EToken.Id Then
            '  変数宣言の場合

            stmt1 = ReadDim(mod1)
        Else

            Select Case CurTkn.TypeTkn
                Case EToken.Imports_
                    stmt1 = ReadImports()

                Case EToken.Module_
                    stmt1 = ReadModule()

                Case EToken.Delegate_
                    GetTkn(EToken.Delegate_)
                    Debug.Assert(CurTkn.TypeTkn = EToken.Function_ OrElse CurTkn.TypeTkn = EToken.Sub_)
                    stmt1 = ReadSubFunction(mod1, True)

                Case EToken.Sub_, EToken.Function_, EToken.Operator_
                    stmt1 = ReadSubFunction(mod1, False)

                Case EToken.End_
                    stmt1 = ReadEnd()

                Case EToken.Var
                    GetTkn(EToken.Var)
                    stmt1 = ReadDim(mod1)

                Case EToken.If_
                    stmt1 = ReadIf()

                Case EToken.Else_
                    stmt1 = ReadElse()

                Case EToken.Return_, EToken.Yield_
                    stmt1 = ReadReturn(CurTkn.TypeTkn)

                Case EToken.Do_
                    stmt1 = ReadDo()

                Case EToken.Loop_
                    stmt1 = ReadLoop()

                Case EToken.Select_
                    stmt1 = ReadSelect()

                Case EToken.Case_
                    stmt1 = ReadCase()

                Case EToken.For_
                    stmt1 = ReadFor()

                Case EToken.Next_
                    stmt1 = ReadNext()

                Case EToken.ElseIf_
                    stmt1 = ReadElseIf()

                Case EToken.Enum_
                    stmt1 = ReadEnum()

                Case EToken.Class_, EToken.Struct, EToken.Interface_
                    stmt1 = ReadClass(mod1)

                Case EToken.Extends
                    stmt1 = ReadInherits()

                Case EToken.Implements_
                    stmt1 = ReadImplements()

                Case EToken.Exit_
                    stmt1 = ReadExit()

                Case EToken.Id, EToken.Base, EToken.CType_, EToken.Dot
                    stmt1 = AssignmentExpression()

                Case EToken.Try_
                    stmt1 = ReadTry()

                Case EToken.Catch_
                    stmt1 = ReadCatch()

                Case EToken.With_
                    stmt1 = ReadWith()

                Case EToken.Throw_
                    stmt1 = ReadThrow()

                Case EToken.ReDim_
                    stmt1 = ReadReDim()

                Case EToken.LineComment
                    stmt1 = ReadLineComment()

                Case EToken.EOT
                Case Else
                    Chk(False)
            End Select
        End If

        Chk(CurTkn Is EOTTkn)

        If stmt1 IsNot Nothing Then
            stmt1.vTknStmt = CurVTkn
        End If

        Return stmt1
    End Function

    Function GetNextStatement() As TStatement
        Dim stmt1 As TStatement

        Do While CurLineIdx < PrjParse.CurSrc.vTextSrc.Length
            stmt1 = PrjParse.CurSrc.StmtSrc(CurLineIdx)
            If stmt1 IsNot Nothing Then

                CurLineStr = PrjParse.CurSrc.vTextSrc(CurLineIdx)
                CurVTkn = PrjParse.CurSrc.LineTkn(CurLineIdx)
                CurLineIdx = CurLineIdx + 1
                Return stmt1
            End If
            CurLineIdx = CurLineIdx + 1
        Loop

        Return Nothing
    End Function

    Function GetStatement(type1 As EToken) As TStatement
        Dim stmt1 As TStatement

        Chk(type1 = EToken.Unknown OrElse CurStmt IsNot Nothing)

        Do While type1 <> EToken.Unknown AndAlso type1 <> EToken.Comment AndAlso CurStmt.TypeStmt = EToken.Comment
            CurStmt = GetNextStatement()
            Debug.Assert(CurStmt IsNot Nothing)
        Loop

        If type1 = EToken.Unknown OrElse type1 = CurStmt.TypeStmt Then

            stmt1 = CurStmt
            Do While True
                CurStmt = GetNextStatement()
                If CurStmt Is Nothing OrElse CurStmt.TypeStmt <> EToken.Imports_ Then
                    Exit Do
                End If
            Loop

            Return stmt1
        Else
            Chk(False)
            Return Nothing
        End If
    End Function

    Public Overrides Sub Parse(src1 As TSourceFile)
        MakeModule(src1)
    End Sub

    Public Sub RegTkn()
        Dim dic1 As New Dictionary(Of String, EToken)

        EOTTkn = NewToken(EToken.EOT, "", 0)

        dic1.Add("Imports", EToken.Imports_)
        dic1.Add("Module", EToken.Module_)
        dic1.Add("OrElse", EToken.OR_)
        dic1.Add("AndAlso", EToken.And_)
        dic1.Add("Not", EToken.Not_)
        dic1.Add("<>", EToken.NE)
        dic1.Add("MustInherit", EToken.Abstract)
        dic1.Add("MustOverride", EToken.MustOverride_)
        dic1.Add("AddressOf", EToken.AddressOf_)
        dic1.Add("Aggregate", EToken.Aggregate_)
        dic1.Add("As", EToken.As_)
        dic1.Add("At", EToken.At_)
        dic1.Add("MyBase", EToken.Base)
        dic1.Add("Break", EToken.Break_)
        dic1.Add("Byval", EToken.ByVal_)
        dic1.Add("Call", EToken.Call_)
        dic1.Add("Case", EToken.Case_)
        dic1.Add("Catch", EToken.Catch_)
        dic1.Add("Class", EToken.Class_)
        dic1.Add("Const", EToken.Const_)
        dic1.Add("CType", EToken.CType_)
        dic1.Add("Default", EToken.Default_)
        dic1.Add("Delegate", EToken.Delegate_)
        dic1.Add("Dim", EToken.Var)
        dic1.Add("Do", EToken.Do_)
        dic1.Add("Each", EToken.Each_)
        dic1.Add("Else", EToken.Else_)
        dic1.Add("ElseIf", EToken.ElseIf_)
        dic1.Add("End", EToken.End_)
        dic1.Add("Enum", EToken.Enum_)
        dic1.Add("Exit", EToken.Exit_)
        dic1.Add("Inherits", EToken.Extends)
        dic1.Add("For", EToken.For_)
        dic1.Add("Foreach", EToken.Foreach_)
        dic1.Add("From", EToken.From_)
        dic1.Add("Function", EToken.Function_)
        dic1.Add("Get", EToken.Get_)
        dic1.Add("GetType", EToken.GetType_)
        dic1.Add("GoTo", EToken.Goto_)
        dic1.Add("Handles", EToken.Handles_)
        dic1.Add("If", EToken.If_)
        dic1.Add("Implements", EToken.Implements_)
        dic1.Add("In", EToken.In_)
        dic1.Add("Interface", EToken.Interface_)
        dic1.Add("Into", EToken.Into_)
        dic1.Add("Is", EToken.Is_)
        dic1.Add("IsNot", EToken.IsNot_)
        dic1.Add("Iterator", EToken.Iterator_)
        dic1.Add("Loop", EToken.Loop_)
        dic1.Add("Namespace", EToken.Namespace_)
        dic1.Add("New", EToken.New_)
        dic1.Add("Next", EToken.Next_)
        dic1.Add("Of", EToken.Of_)
        dic1.Add("Operator", EToken.Operator_)
        dic1.Add("Out", EToken.Out_)
        dic1.Add("Overrides", EToken.Override)
        dic1.Add("ParamArray", EToken.ParamArray_)
        dic1.Add("Partial", EToken.Partial_)
        dic1.Add("Public", EToken.Public_)
        dic1.Add("Protected", EToken.Protected_)
        dic1.Add("Friend", EToken.Friend_)
        dic1.Add("Private", EToken.Private_)
        dic1.Add("ByRef", EToken.Ref)
        dic1.Add("ReDim", EToken.ReDim_)
        dic1.Add("Return", EToken.Return_)
        dic1.Add("Set", EToken.Set_)
        dic1.Add("Select", EToken.Select_)
        dic1.Add("Shared", EToken.Shared_)
        dic1.Add("Step", EToken.Step_)
        dic1.Add("Structure", EToken.Struct)
        dic1.Add("Sub", EToken.Sub_)
        dic1.Add("Take", EToken.Take_)
        dic1.Add("Then", EToken.Then_)
        dic1.Add("Throw", EToken.Throw_)
        dic1.Add("To", EToken.To_)
        dic1.Add("Try", EToken.Try_)
        dic1.Add("TypeOf", EToken.Instanceof)
        dic1.Add("Overridable", EToken.Virtual)
        dic1.Add("Where", EToken.Where_)
        dic1.Add("While", EToken.While_)
        dic1.Add("With", EToken.With_)
        dic1.Add("Yield", EToken.Yield_)
        dic1.Add("@id", EToken.Id)
        dic1.Add("@int", EToken.Int)
        dic1.Add("@hex", EToken.Hex)
        dic1.Add("/*", EToken.BlockComment)
        dic1.Add("'", EToken.LineComment)
        dic1.Add("=", EToken.Eq)
        dic1.Add("+=", EToken.ADDEQ)
        dic1.Add("-=", EToken.SUBEQ)
        dic1.Add("*=", EToken.MULEQ)
        dic1.Add("/=", EToken.DIVEQ)
        dic1.Add("%=", EToken.MODEQ)
        dic1.Add("+", EToken.ADD)
        dic1.Add("-", EToken.Mns)
        dic1.Add("Mod", EToken.MOD_)
        dic1.Add("And", EToken.Anp)
        dic1.Add("(", EToken.LP)
        dic1.Add(")", EToken.RP)
        dic1.Add("*", EToken.MUL)
        dic1.Add(",", EToken.Comma)
        dic1.Add(".", EToken.Dot)
        dic1.Add("/", EToken.DIV)
        dic1.Add(":", EToken.MMB)
        dic1.Add("", EToken.SM)
        dic1.Add("[", EToken.LB)
        dic1.Add("]", EToken.RB)
        dic1.Add("_", EToken.LowLine)
        dic1.Add("^", EToken.HAT)
        dic1.Add("{", EToken.LC)
        dic1.Add("|", EToken.BitOR)
        dic1.Add("}", EToken.RC)
        dic1.Add("~", EToken.Tilde)
        dic1.Add("<", EToken.LT)
        dic1.Add(">", EToken.GT)
        dic1.Add("<=", EToken.LE)
        dic1.Add(">=", EToken.GE)

        ' for Add
        For Each key1 In dic1.Keys
            vTkn.Add(key1.ToLower(), dic1(key1))
        Next

        If vTknName Is Nothing Then
            vTknName = New Dictionary(Of EToken, String)()
            ' for Add
            For Each key1 In dic1.Keys
                vTknName.Add(dic1(key1), key1)
            Next
            vTknName.Add(EToken.ASN, "=")
            vTknName.Add(EToken.INC, "++")
            vTknName.Add(EToken.DEC, "--")


            vTknName.Add(EToken.ExitFor, "Exit For")
            vTknName.Add(EToken.ExitDo, "Exit Do")
            vTknName.Add(EToken.ExitSub, "Exit Sub")

            vTknName.Add(EToken.EndIf_, "End If")
            vTknName.Add(EToken.EndSub, "End Sub")
            vTknName.Add(EToken.EndFunction, "End Function")
            vTknName.Add(EToken.EndOperator, "End Operator")
            vTknName.Add(EToken.EndClass, "End Class")
            vTknName.Add(EToken.EndStruct, "End Structure")
            vTknName.Add(EToken.EndInterface, "End Interface")
            vTknName.Add(EToken.EndEnum, "End Enum")
            vTknName.Add(EToken.EndModule, "End Module")
            vTknName.Add(EToken.EndSelect, "End Select")
            vTknName.Add(EToken.EndTry, "End Try")
            vTknName.Add(EToken.EndWith, "End With")
        End If
    End Sub

    Function NewToken(type1 As EToken, str1 As String, pos1 As Integer) As TToken
        Dim tkn1 As New TToken

        tkn1.TypeTkn = type1
        tkn1.StrTkn = str1
        tkn1.PosTkn = pos1

        Return tkn1
    End Function

    Public Overrides Function Lex(src_text As String) As TList(Of TToken)
        Dim v1 As TList(Of TToken)
        Dim cur1 As Integer, spc As Integer
        Dim src_len As Integer
        Dim k1 As Integer
        Dim ch1 As Char
        Dim ch2 As Char
        Dim str1 As String = Nothing
        Dim type1 As EToken
        Dim prv_type As EToken
        Dim tkn1 As TToken
        Dim sb1 As TStringWriter
        Dim ok As Boolean

        src_len = src_text.Length
        v1 = New TList(Of TToken)()

        cur1 = 0
        prv_type = EToken.Unknown

        Do While True
            tkn1 = Nothing

            spc = 0
            Do While cur1 < src_len
                ch1 = src_text(cur1)
                Select Case ch1
                    Case " "c
                        spc += 1
                    Case vbTab
                        spc += 4
                    Case Else
                        Exit Do
                End Select
                cur1 += 1
            Loop
            If src_len <= cur1 Then
                Exit Do
            End If

            ch1 = src_text(cur1)
            If cur1 + 1 < src_text.Length Then
                ch2 = src_text(cur1 + 1)
            Else
                ch2 = ChrW(0)
            End If

            Select Case ch1
                Case """"c
                    '  引用符の場合

                    sb1 = New TStringWriter()
                    k1 = cur1 + 1
                    Do While k1 < src_text.Length
                        ch2 = src_text(k1)
                        If ch2 = """"c Then
                            If k1 + 1 < src_text.Length AndAlso src_text(k1 + 1) = """"c Then
                                '  引用符のエスケープの場合

                                sb1.Append(""""c)
                                k1 = k1 + 2
                            Else
                                If k1 + 1 < src_text.Length AndAlso src_text(k1 + 1) = "c"c Then
                                    '  文字の場合

                                    tkn1 = New TToken(EToken.Char_, sb1.ToString(), cur1)
                                    cur1 = k1 + 2
                                Else
                                    '  文字列の場合

                                    tkn1 = NewToken(EToken.String_, sb1.ToString(), cur1)
                                    cur1 = k1 + 1
                                End If
                                Exit Do
                            End If
                        Else
                            sb1.Append(ch2)
                            k1 = k1 + 1
                        End If
                    Loop

                Case "'"c
                    '  コメントの場合
                    tkn1 = NewToken(EToken.LineComment, src_text.Substring(cur1 + 1), cur1)
                    cur1 = src_text.Length

                Case Else
                    If Char.IsDigit(ch1) Then
                        '  数字の場合

                        ' for Find
                        For k1 = cur1 + 1 To src_text.Length - 1
                            ch2 = src_text(k1)
                            If Not Char.IsDigit(ch2) AndAlso ch2 <> "."c Then
                                Exit For
                            End If
                        Next
                        If k1 < src_text.Length AndAlso src_text(k1) = "F"c Then
                            k1 = k1 + 1
                        End If

                        str1 = TSys.Substring(src_text, cur1, k1)
                        tkn1 = NewToken(EToken.Int, str1, cur1)

                        cur1 = k1
                    ElseIf ch1 = "&"c AndAlso ch2 = "H"c Then
                        ' 16進数の場合

                        ' for Find
                        For k1 = cur1 + 2 To src_text.Length - 1
                            ch2 = src_text(k1)
                            If Not (Char.IsDigit(ch2) OrElse "A"c <= ch2 AndAlso ch2 <= "F"c) Then
                                Exit For
                            End If
                        Next

                        str1 = TSys.Substring(src_text, cur1, k1)
                        tkn1 = NewToken(EToken.Hex, str1, cur1)

                        cur1 = k1
                    ElseIf 256 <= AscW(ch1) OrElse Char.IsLetter(ch1) OrElse ch1 = "_"c Then
                        '  英字の場合

                        ' for Find
                        For k1 = cur1 + 1 To src_text.Length - 1

                            ch2 = src_text(k1)
                            If AscW(ch2) < 256 AndAlso Not Char.IsLetterOrDigit(ch2) AndAlso ch2 <> "_"c Then
                                '  半角で英数字や"_"でない場合

                                Exit For
                            End If
                        Next
                        str1 = TSys.Substring(src_text, cur1, k1)
                        If vTkn.ContainsKey(str1.ToLower()) Then
                            '  予約語の場合

                            type1 = vTkn(str1.ToLower())
                            If type1 = EToken.GetType_ AndAlso (prv_type = EToken.Dot OrElse prv_type = EToken.Function_) Then
                                type1 = EToken.Id
                            ElseIf type1 = EToken.Select_ AndAlso prv_type = EToken.Dot Then
                                type1 = EToken.Id
                            End If
                        Else
                            '  識別子の場合

                            type1 = EToken.Id
                        End If
                        tkn1 = NewToken(type1, str1, cur1)

                        cur1 = k1
                    Else
                        '  記号の場合

                        ok = False
                        type1 = EToken.Unknown
                        If cur1 + 1 < src_len Then
                            '  2文字の記号を調べる

                            str1 = TSys.Substring(src_text, cur1, cur1 + 2)
                            ok = vTkn.ContainsKey(str1)
                            If ok Then
                                type1 = vTkn(str1)
                            End If
                        End If
                        If Not ok Then
                            '  1文字の記号を調べる

                            str1 = TSys.Substring(src_text, cur1, cur1 + 1)
                            If vTkn.ContainsKey(str1) Then
                                type1 = vTkn(str1)
                            Else
                                '  ない場合

                                Debug.WriteLine("lex str err")
                                Chk(False)
                            End If
                        End If

                        tkn1 = NewToken(type1, str1, cur1)
                        cur1 = cur1 + str1.Length
                    End If
            End Select

            ' Debug.WriteLine("token:{0} {1}", tkn1.StrTkn, tkn1.TypeTkn)
            tkn1.SpcTkn = spc
            v1.Add(tkn1)
            prv_type = tkn1.TypeTkn
        Loop

        Return v1
    End Function

    Public Overrides Sub RegAllClass(prj1 As TProject, src1 As TSourceFile)
        Dim id1 As TToken, k1 As Integer, cla1 As TClass, cla2 As TClass, id2 As TToken, is_delegate As Boolean

        For Each v In src1.LineTkn

            If 3 <= v.Count AndAlso v(0).TypeTkn = EToken.Public_ Then

                is_delegate = False
                Select Case v(1).TypeTkn
                    Case EToken.Delegate_
                        Debug.Assert(v(2).TypeTkn = EToken.Sub_ OrElse v(2).TypeTkn = EToken.Function_)
                        is_delegate = True
                        k1 = 3

                    Case EToken.Class_, EToken.Struct, EToken.Interface_, EToken.Enum_
                        k1 = 2
                    Case EToken.Abstract
                        Select Case v(2).TypeTkn
                            Case EToken.Class_, EToken.Struct, EToken.Interface_
                                k1 = 3
                            Case Else
                                Debug.Assert(False)
                        End Select
                    Case Else
                        k1 = -1
                End Select

                If k1 <> -1 Then
                    id1 = v(k1)

                    If is_delegate Then
                        Debug.Assert(prj1.GetCla(id1.StrTkn) Is Nothing)

                        cla1 = New TDelegate(prj1, id1.StrTkn)
                        prj1.SimpleParameterizedClassList.Add(cla1)
                        prj1.SimpleParameterizedClassTable.Add(cla1.NameCla(), cla1)
                    Else
                        cla1 = prj1.RegCla(id1.StrTkn)
                    End If

                    If k1 + 2 < v.Count AndAlso v(k1 + 1).TypeTkn = EToken.LP AndAlso v(k1 + 2).TypeTkn = EToken.Of_ Then
                        cla1.GenericType = EGeneric.ParameterizedClass

                        cla1.GenCla = New TList(Of TClass)()

                        k1 += 3
                        Do While k1 < v.Count
                            id2 = v(k1)

                            cla2 = New TClass(prj1, id2.StrTkn)
                            cla2.IsParamCla = True
                            cla2.GenericType = EGeneric.ArgumentClass
                            cla1.GenCla.Add(cla2)

                            If v(k1 + 1).TypeTkn = EToken.RP Then
                                Debug.Assert(is_delegate OrElse k1 + 2 = v.Count)
                                Exit Do
                            End If

                            Debug.Assert(v(k1 + 1).TypeTkn = EToken.Comma)
                            k1 += 2
                        Loop

                        prj1.dicCmpCla.Add(cla1, New TList(Of TClass)())
                    Else
                        cla1.GenericType = EGeneric.SimpleClass
                    End If
                End If
            End If
        Next
    End Sub

    Function ArgumentExpressionList(app1 As TApply) As TApply
        Dim trm1 As TTerm

        ' 			bool	b_of;
        '             b_of = false;
        GetTkn(EToken.LP)
        If CurTkn.TypeTkn = EToken.Of_ Then
            GetTkn(EToken.Of_)
        End If
        '                 b_of = true;
        If CurTkn.TypeTkn <> EToken.RP Then
            Do While True
                trm1 = TermExpression()
                app1.AddInArg(trm1)

                If CurTkn.TypeTkn <> EToken.Comma Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If
        GetTkn(EToken.RP)

        Return app1
    End Function

    Function CallExpression(trm1 As TTerm) As TTerm


        Do While CurTkn.TypeTkn = EToken.LP
            Dim app1 As TApply = TApply.MakeAppCall(trm1)
            ArgumentExpressionList(app1)
            trm1 = app1
        Loop

        Return trm1
    End Function

    '   配列の構文解析
    Function ArrayExpression() As TArray
        Dim arr1 As TArray
        Dim trm1 As TTerm

        arr1 = New TArray()
        GetTkn(EToken.LC)
        If CurTkn.TypeTkn <> EToken.RC Then
            Do While True
                trm1 = TermExpression()
                arr1.TrmArr.Add(trm1)
                If CurTkn.TypeTkn = EToken.RC Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If
        GetTkn(EToken.RC)

        Return arr1
    End Function


    Function NewExpression() As TApply
        Dim tkn1 As TToken
        Dim type1 As TClass
        Dim app1 As TApply

        tkn1 = GetTkn(EToken.New_)
        type1 = ReadType(True)
        app1 = TApply.MakeAppNew(type1)
        If CurTkn.TypeTkn = EToken.LP Then
            ArgumentExpressionList(app1)
        End If
        If CurTkn.TypeTkn = EToken.LC Then
            ' 配列の場合
            app1.IniApp = ArrayExpression()

            ' 配列型に変える
            app1.NewApp = PrjParse.GetArrCla(app1.NewApp, 1)
        End If
        If CurTkn.TypeTkn = EToken.From_ Then
            GetTkn(EToken.From_)

            Debug.Assert(CurTkn.TypeTkn = EToken.LC)
            app1.IniApp = ArrayExpression()
        End If

        Return app1
    End Function

    ' From i In v1 Where i Mod 2 = 0 Select AA(i)
    Function FromExpression() As TFrom
        Dim from1 As New TFrom

        GetTkn(EToken.From_)

        Dim id1 As TToken = GetTkn(EToken.Id)
        from1.VarQry = New TLocalVariable(id1, Nothing)

        GetTkn(EToken.In_)
        from1.SeqQry = TermExpression()

        If CurTkn.TypeTkn = EToken.Where_ Then

            GetTkn(EToken.Where_)
            from1.CndQry = TermExpression()
        End If

        If CurTkn.TypeTkn = EToken.Select_ Then

            GetTkn(EToken.Select_)
            from1.SelFrom = TermExpression()
        End If

        If CurTkn.TypeTkn = EToken.Take_ Then

            GetTkn(EToken.Take_)
            from1.TakeFrom = TermExpression()
        End If


        If CurTkn.TypeTkn = EToken.From_ Then
            from1.InnerFrom = FromExpression()
        End If

        Return from1
    End Function

    ' Aggregate x In v Into Sum(x.Value)
    Function AggregateExpression() As TAggregate
        Dim aggr1 As New TAggregate, id1 As TToken, id2 As TToken

        GetTkn(EToken.Aggregate_)
        id1 = GetTkn(EToken.Id)
        aggr1.VarQry = New TLocalVariable(id1, Nothing)

        GetTkn(EToken.In_)
        aggr1.SeqQry = TermExpression()

        If CurTkn.TypeTkn = EToken.Where_ Then

            GetTkn(EToken.Where_)
            aggr1.CndQry = TermExpression()
        End If

        GetTkn(EToken.Into_)

        id2 = GetTkn(EToken.Id)
        Select Case id2.StrTkn
            Case "Sum"
                aggr1.FunctionAggr = EAggregateFunction.eSum
            Case "Max"
                aggr1.FunctionAggr = EAggregateFunction.eMax
            Case "Min"
                aggr1.FunctionAggr = EAggregateFunction.eMin
            Case "Average"
                aggr1.FunctionAggr = EAggregateFunction.eAverage
            Case Else
                Debug.Assert(False)
        End Select


        GetTkn(EToken.LP)

        aggr1.IntoAggr = TermExpression()

        GetTkn(EToken.RP)

        Return aggr1
    End Function

    Function PrimaryExpression() As TTerm
        Dim ref1 As TReference
        Dim trm1 As TTerm, trm2 As TTerm
        Dim ret1 As TTerm
        Dim id1 As TToken, tkn1 As TToken
        Dim type1 As TClass
        Dim app1 As TApply

        Select Case CurTkn.TypeTkn
            Case EToken.Id
                id1 = GetTkn(EToken.Id)
                If CurTkn.TypeTkn = EToken.LP AndAlso NxtTkn.TypeTkn = EToken.Of_ Then
                    ' ジェネリック型の場合

                    ' ジェネリック型の構文解析
                    type1 = ReadGenType(id1)
                    ref1 = New TReference(type1)
                    Return ref1
                End If

                ref1 = New TReference(id1)
                Return CallExpression(ref1)

            Case EToken.Dot
                trm1 = Nothing
                Do While CurTkn.TypeTkn = EToken.Dot
                    GetTkn(EToken.Dot)
                    id1 = GetTkn(EToken.Id)
                    trm2 = New TDot(trm1, id1.StrTkn)
                    trm1 = CallExpression(trm2)
                Loop

                Return trm1

            Case EToken.Base
                GetTkn(EToken.Base)
                GetTkn(EToken.Dot)
                Debug.Assert(CurTkn.TypeTkn = EToken.New_ OrElse CurTkn.TypeTkn = EToken.Id)
                tkn1 = GetTkn(EToken.Unknown)
                app1 = TApply.MakeAppBase(tkn1)
                ArgumentExpressionList(app1)
                Return app1

            Case EToken.LP
                GetTkn(EToken.LP)
                trm1 = New TParenthesis(TermExpression())
                GetTkn(EToken.RP)
                ret1 = CallExpression(trm1)

            Case EToken.LC
                Return ArrayExpression()

            Case EToken.String_, EToken.Char_, EToken.Int, EToken.Hex
                tkn1 = GetTkn(EToken.Unknown)
                ret1 = New TConstant(tkn1.TypeTkn, tkn1.StrTkn)

            Case EToken.New_
                Return NewExpression()

            Case EToken.Instanceof
                GetTkn(EToken.Instanceof)
                trm1 = AdditiveExpression()
                GetTkn(EToken.Is_)
                type1 = ReadType(False)
                Return TApply.NewTypeOf(trm1, type1)

            Case EToken.GetType_
                GetTkn(EToken.GetType_)
                GetTkn(EToken.LP)
                type1 = ReadType(False)
                GetTkn(EToken.RP)
                Return TApply.MakeAppGetType(type1)

            Case EToken.CType_
                GetTkn(EToken.CType_)
                GetTkn(EToken.LP)
                trm1 = AdditiveExpression()
                GetTkn(EToken.Comma)
                trm1.CastType = ReadType(False)
                GetTkn(EToken.RP)

                Return trm1

            Case EToken.AddressOf_
                GetTkn(EToken.AddressOf_)
                trm1 = TermExpression()
                Debug.Assert(TypeOf trm1 Is TReference)
                CType(trm1, TReference).IsAddressOf = True
                Return trm1

            Case EToken.From_
                Return FromExpression()

            Case EToken.Aggregate_
                Return AggregateExpression()

            Case EToken.If_
                GetTkn(EToken.If_)
                GetTkn(EToken.LP)
                Dim cnd1 As TTerm = TermExpression()
                GetTkn(EToken.Comma)
                trm1 = TermExpression()
                GetTkn(EToken.Comma)
                trm2 = TermExpression()
                GetTkn(EToken.RP)

                app1 = TApply.MakeApp3Opr(cnd1, trm1, trm2)
                Return app1

            Case Else
                Chk(False)
                Return Nothing
        End Select

        Return ret1
    End Function

    Function DotExpression() As TTerm
        Dim trm1 As TTerm
        Dim id1 As TToken

        trm1 = PrimaryExpression()

        Do While CurTkn.TypeTkn = EToken.Dot
            GetTkn(EToken.Dot)
            id1 = GetTkn(EToken.Id)
            Dim dot1 = New TDot(trm1, id1.StrTkn)
            trm1 = CallExpression(dot1)
        Loop

        Return trm1
    End Function

    Function UnaryExpression() As TTerm
        Dim tkn1 As TToken
        Dim trm1 As TTerm

        If CurTkn.TypeTkn = EToken.Mns Then
            tkn1 = GetTkn(EToken.Mns)
            trm1 = DotExpression()

            Return TApply.MakeApp1Opr(tkn1, trm1)
        End If

        Return DotExpression()
    End Function

    Function MultiplicativeExpression() As TTerm
        Dim trm1 As TTerm
        Dim tkn1 As TToken
        Dim trm2 As TTerm

        trm1 = UnaryExpression()
        If CurTkn.TypeTkn = EToken.MUL OrElse CurTkn.TypeTkn = EToken.DIV OrElse CurTkn.TypeTkn = EToken.MOD_ Then
            tkn1 = GetTkn(EToken.Unknown)
            trm2 = MultiplicativeExpression()

            Return TApply.MakeApp2Opr(tkn1, trm1, trm2)
        End If

        Return trm1
    End Function

    Public Function AdditiveExpression() As TTerm
        Dim trm1 As TTerm
        Dim tkn1 As TToken
        Dim trm2 As TTerm

        trm1 = MultiplicativeExpression()
        If CurTkn.TypeTkn = EToken.ADD OrElse CurTkn.TypeTkn = EToken.Mns Then
            tkn1 = GetTkn(EToken.Unknown)
            trm2 = AdditiveExpression()

            Return TApply.MakeApp2Opr(tkn1, trm1, trm2)
        End If
        Return trm1
    End Function

    Public Function RelationalExpression() As TTerm
        Dim trm1 As TTerm
        Dim trm2 As TTerm
        Dim type1 As EToken
        '      Dim type2 As TClass
        'Dim par1 As Boolean

        trm1 = AdditiveExpression()
        Select Case CurTkn.TypeTkn
            Case EToken.Eq, EToken.ADDEQ, EToken.SUBEQ, EToken.MULEQ, EToken.DIVEQ, EToken.MODEQ, EToken.NE, EToken.LT, EToken.GT, EToken.LE, EToken.GE, EToken.Is_, EToken.IsNot_
                type1 = CurTkn.TypeTkn
                GetTkn(EToken.Unknown)
                trm2 = AdditiveExpression()
                Return TApply.NewOpr2(type1, trm1, trm2)

            Case Else
                Return trm1
        End Select
    End Function

    Function NotExpression() As TTerm
        Dim trm1 As TTerm
        Dim type1 As EToken
        Dim app1 As TApply

        If CurTkn.TypeTkn = EToken.Not_ Then
            type1 = CurTkn.TypeTkn
            GetTkn(type1)
            trm1 = NotExpression()
            Debug.Assert(TypeOf trm1 Is TApply OrElse TypeOf trm1 Is TReference OrElse TypeOf trm1 Is TParenthesis)
            If TypeOf trm1 Is TApply Then
                app1 = CType(trm1, TApply)
                app1.Negation = Not app1.Negation
                Return app1
            Else
                Dim opr1 As TApply = TApply.NewOpr(type1)

                opr1.AddInArg(trm1)
                Return opr1
            End If
        End If

        Return RelationalExpression()
    End Function

    Function AndExpression() As TTerm
        Dim trm1 As TTerm
        Dim opr1 As TApply
        Dim type1 As EToken

        trm1 = NotExpression()
        If CurTkn.TypeTkn = EToken.And_ OrElse CurTkn.TypeTkn = EToken.Anp Then

            type1 = CurTkn.TypeTkn
            opr1 = TApply.NewOpr(type1)
            opr1.AddInArg(trm1)
            Do While CurTkn.TypeTkn = type1
                GetTkn(type1)
                opr1.AddInArg(NotExpression())
            Loop

            Return opr1
        Else

            Return trm1
        End If
    End Function

    Function OrExpression() As TTerm
        Dim trm1 As TTerm
        Dim opr1 As TApply
        Dim type1 As EToken

        trm1 = AndExpression()
        If CurTkn.TypeTkn = EToken.OR_ Then

            type1 = CurTkn.TypeTkn
            opr1 = TApply.NewOpr(type1)
            opr1.AddInArg(trm1)
            Do While CurTkn.TypeTkn = type1
                GetTkn(type1)
                opr1.AddInArg(AndExpression())
            Loop

            Return opr1
        Else

            Return trm1
        End If
    End Function

    Public Function TermExpression() As TTerm
        Return OrExpression()
    End Function

    Public Function AssignmentExpression() As TStatement
        Dim trm1 As TTerm
        Dim trm2 As TTerm
        Dim rel1 As TApply
        Dim asn1 As TAssignment
        Dim eq1 As TToken

        trm1 = CType(AdditiveExpression(), TTerm)

        Select Case CurTkn.TypeTkn
            Case EToken.Eq, EToken.ADDEQ, EToken.SUBEQ, EToken.MULEQ, EToken.DIVEQ, EToken.MODEQ
                eq1 = GetTkn(EToken.Unknown)
                trm2 = CType(TermExpression(), TTerm)
                rel1 = TApply.NewOpr2(eq1.TypeTkn, trm1, trm2)
                asn1 = New TAssignment(rel1)
                Return asn1
        End Select

        If TypeOf trm1 Is TReference Then
            Return New TEnumElement(CType(trm1, TReference))
        End If
        Return New TCall(CType(trm1, TApply))
    End Function

End Class

Public Class TClassStatement
    Inherits TStatement
    Public KndClaStmt As EClass = EClass.eClassCla
    Public ClaClaStmt As TClass
End Class

Public Class TInheritsStatement
    Inherits TStatement
    Public ClassNameInheritsStmt As String
    Public ParamName As TList(Of String)
End Class

Public Class TImplementsStatement
    Inherits TStatement
    Public ClassImplementsStmt As New TList(Of TClass)
End Class

Public Class TEnumStatement
    Inherits TStatement
    Public NameEnumStmt As String
End Class

Public Class TFunctionStatement
    Inherits TStatement
    Public ModifierFncStmt As TModifier
    Public OpFncStmt As EToken = EToken.Unknown
    Public NameFncStmt As String
    Public ArgumentClassFncStmt As TList(Of TClass)
    Public ArgumentFncStmt As New TList(Of TVariable)
    Public RetType As TClass
    Public InterfaceFncStmt As TClass
    Public InterfaceFncName As String
    Public IsDelegateFncStmt As Boolean
End Class

Public Class TIfStatement
    Inherits TStatement
    Public CndIfStmt As TTerm
End Class

Public Class TSelectStatement
    Inherits TStatement
    Public TermSelectStatement As TTerm
End Class

Public Class TCaseStatement
    Inherits TStatement
    Public IsCaseElse As Boolean
    Public TermCaseStmt As New TList(Of TTerm)
End Class

Public Class TCatchStatement
    Inherits TStatement
    Public VariableCatchStmt As TVariable
End Class

Public Class TForStatement
    Inherits TStatement
    Public IdxForStmt As TReference
    Public FromForStmt As TTerm
    Public ToForStmt As TTerm
    Public StepForStmt As TTerm
    Public InVarForStmt As TVariable
    Public InTrmForStmt As TTerm
End Class

Public Class TExit
    Inherits TStatement
    Public LabelExit As Integer

    Public Sub New()
    End Sub
End Class

Public Class TThrow
    Inherits TStatement
    Public TrmThrow As TTerm

    Public Sub New(trm1 As TTerm)
        TypeStmt = EToken.Throw_
        TrmThrow = trm1
    End Sub

End Class

Public Class TReDim
    Inherits TStatement
    Public TrmReDim As TTerm
    Public DimReDim As TList(Of TTerm)

    Public Sub New(trm1 As TTerm, vtrm1 As TList(Of TTerm))
        TypeStmt = EToken.ReDim_
        TrmReDim = trm1
        DimReDim = vtrm1
    End Sub
End Class

Public Class TImports
    Inherits TStatement
End Class

Public Class TModule
    Inherits TStatement
    Public NameMod As String
End Class

Public Class TElseIf
    Inherits TStatement
    Public CndElseIf As TTerm
End Class

Public Class TDoStmt
    Inherits TStatement

    Public CndDo As TTerm
End Class

Public Class TEnumElement
    Inherits TStatement

    Public NameEnumEle As String

    Public Sub New(ref1 As TReference)
        TypeStmt = EToken.Id
        NameEnumEle = ref1.NameRef
    End Sub

End Class

Public Class TComment
    Inherits TStatement

    Public LineCom As New TList(Of String)

    Public Sub New()
        TypeStmt = EToken.Comment
    End Sub

    Public Function GetFirstLine() As String
        For Each s In LineCom
            If s <> "" Then
                Return s
            End If
        Next

        Return ""
    End Function
End Class

Public Class TError
    Inherits Exception

    Public MsgErr As String

    Public Sub New(msg As String)
        Debug.WriteLine("err:" + msg)
        MsgErr = msg
    End Sub
End Class
