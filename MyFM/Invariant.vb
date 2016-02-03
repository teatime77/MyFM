Imports System.Diagnostics

Public Class TTokenWriter
    Public ObjTlm As Object
    Public ParserTW As TSourceParser
    Public TokenListTW As New List(Of TToken)

    Public Sub New(obj As Object, parser As TSourceParser)
        ObjTlm = obj
        ParserTW = parser
    End Sub

    Sub AddToken(obj As Object)
        TokenListTW.Add(New TToken(obj))
    End Sub

    Public Sub AddToken(type1 As EToken, obj As Object)
        TokenListTW.Add(New TToken(type1, obj))
    End Sub

    Public Sub TAB(n As Integer)
        Dim tab1 As New TToken

        tab1.TypeTkn = EToken.Tab
        tab1.TabTkn = n
        TokenListTW.Add(tab1)
    End Sub

    Public Sub Fmt(ParamArray args As Object())
        For Each o1 In args

            If TypeOf o1 Is String Then
                TokenListTW.Add(New TToken(CType(o1, String), o1))

            ElseIf TypeOf o1 Is Integer Then
                TokenListTW.Add(New TToken(EToken.Int, o1.ToString()))

            ElseIf TypeOf o1 Is TDot Then
                AddToken(o1)

            ElseIf TypeOf o1 Is TReference Then
                AddToken(o1)

            ElseIf TypeOf o1 Is TClass Then
                AddToken(o1)

            ElseIf TypeOf o1 Is TVariable Then
                AddToken(o1)

            ElseIf TypeOf o1 Is TToken Then
                TokenListTW.Add(CType(o1, TToken))

            ElseIf TypeOf o1 Is EToken Then
                Dim type1 As EToken = CType(o1, EToken)

                If type1 = EToken.NL OrElse type1 = EToken.EOL Then
                    Dim new_line As New TToken

                    If type1 = EToken.EOL Then

                        Select Case ParserTW.LanguageSP
                            Case ELanguage.Basic
                            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                AddToken(EToken.SM, ObjTlm)
                        End Select
                    End If

                    new_line.TypeTkn = EToken.NL
                    TokenListTW.Add(new_line)
                Else
                    If ParserTW.vTknName.ContainsKey(type1) Then

                        Dim s As String = ParserTW.vTknName(type1)
                        If s <> "" Then
                            AddToken(type1, ObjTlm)
                        End If
                    End If
                End If

            ElseIf TypeOf o1 Is List(Of TToken) Then
                TokenListTW.AddRange(CType(o1, List(Of TToken)))

            Else
                Debug.Assert(False)
            End If
        Next

    End Sub

    Public Function GetTokenList() As List(Of TToken)
        Return TokenListTW
    End Function
End Class

Public Class TNaviMakeSourceCode
    Inherits TDeclarative
    Public PrjMK As TProject
    Public ParserMK As TSourceParser

    Public Sub New(prj1 As TProject, parser As TSourceParser)
        PrjMK = prj1
        ParserMK = parser
    End Sub

    Public Sub VariableTypeInitializer(self As Object, tw As TTokenWriter)
        If TypeOf self Is TVariable Then
            With CType(self, TVariable)
                Dim as_new As Boolean = False, app1 As TApply

                If .InitVar IsNot Nothing AndAlso .InitVar.TokenList Is Nothing Then
                    ' 生成された初期化関数が、まだ呼ばれていない場合

                    NaviTerm(.InitVar)
                End If

                If .TypeVar IsNot Nothing AndAlso Not .NoType AndAlso ParserMK.LanguageSP <> ELanguage.JavaScript Then
                    tw.Fmt(EToken.As_)
                    If .InitVar IsNot Nothing AndAlso .InitVar.IsApp() AndAlso CType(.InitVar, TApply).TypeApp = EToken.New_ Then
                        as_new = True

                        app1 = CType(.InitVar, TApply)
                        If app1.ArgApp.Count = 0 Then
                            ' 引数がない場合

                            tw.Fmt(EToken.New_)
                            tw.Fmt(app1.NewApp.TokenListVar)
                        Else
                            ' 引数がある場合
                            tw.Fmt(app1.TokenList)
                        End If

                        If app1.IniApp IsNot Nothing Then

                            tw.Fmt(EToken.From_, app1.IniApp.TokenList)
                        End If
                    Else
                        PrjMK.SetClassNameList(.TypeVar, ParserMK)
                        tw.Fmt(.TypeVar.TokenListVar)
                    End If
                End If

                If Not as_new AndAlso .InitVar IsNot Nothing Then
                    tw.Fmt(EToken.ASN, .InitVar.TokenList)
                End If

            End With
        End If
    End Sub

    ' コメントのソースを作る
    Public Sub ComSrc(com1 As TComment, tab1 As Integer, tw As TTokenWriter)
        If com1 IsNot Nothing Then
            For Each s In com1.LineCom
                If s <> "" Then
                    tw.TAB(tab1)
                    tw.Fmt(New TToken(EToken.Comment, s))
                End If
                tw.Fmt(EToken.NL)
            Next
        End If
    End Sub

    Public Sub JavaScriptClass(self As Object, tw As TTokenWriter)
        If TypeOf self Is TClass Then
            With CType(self, TClass)
                Select Case .KndCla
                    Case EClass.EnumCla
                        '  列挙型の場合

                        tw.Fmt(EToken.Var, .NameVar, EToken.ASN, EToken.Function_, EToken.LP, EToken.RP, EToken.LC, EToken.RC, EToken.NL)

                        Dim idx As Integer = 0
                        For Each fld1 In .FldCla
                            tw.Fmt(.NameVar, EToken.Dot, fld1.NameVar, EToken.ASN, New TToken(EToken.Int, idx.ToString()), EToken.EOL)
                            idx += 1
                        Next

                    Case EClass.StructCla, EClass.ClassCla
                        ' 構造体かクラスの場合

                        If .DirectSuperClassList.Count <> 0 AndAlso .DirectSuperClassList(0).NameVar = "Attribute" Then
                        Else
                            '  すべてのメソッドに対し
                            For Each fnc1 In .FncCla
                                tw.Fmt(fnc1.TokenListVar)
                            Next
                        End If

                End Select
            End With
        End If
    End Sub

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TStatement Then
            With CType(self, TStatement)
                Dim obj As Object = Sys.UpStatementFunction(CType(self, TStatement).UpTrm)

                If obj Is Nothing Then

                ElseIf TypeOf obj Is TStatement Then
                    Dim stmt1 As TStatement = CType(obj, TStatement)

                    If TypeOf stmt1 Is TIfBlock Then
                        If CType(stmt1, TIfBlock).WithIf IsNot Nothing Then
                            .TabStmt = stmt1.TabStmt + 1
                        Else
                            .TabStmt = stmt1.TabStmt
                        End If
                    ElseIf TypeOf stmt1 Is TFor OrElse TypeOf stmt1 Is TCase Then
                        .TabStmt = stmt1.TabStmt + 1
                    ElseIf TypeOf stmt1 Is TIf OrElse TypeOf stmt1 Is TSelect OrElse TypeOf stmt1 Is TTry Then
                        .TabStmt = stmt1.TabStmt
                    Else
                        .TabStmt = stmt1.TabStmt
                    End If

                ElseIf TypeOf obj Is TFunction Then
                    Dim fnc As TFunction = CType(obj, TFunction)

                    If fnc.ComVar Is self Then
                        .TabStmt = 1
                    Else

                        If fnc.WithFnc IsNot Nothing Then
                            .TabStmt = 3
                        Else
                            .TabStmt = 2
                        End If
                    End If
                Else
                End If
            End With
        End If
    End Sub

    Public Overrides Sub EndCondition(self As Object)
        Dim tw As New TTokenWriter(self, ParserMK)

        If self Is Nothing Then
            tw.Fmt("null stmt", EToken.NL)
            Exit Sub

        End If

        If TypeOf self Is TVariable Then
            With CType(self, TVariable)
                If TypeOf self Is TClass Then
                    With CType(self, TClass)
                        Dim gen_tw As New TTokenWriter(self, ParserMK)

                        ComSrc(CType(.ComCla(), TComment), 0, tw)
                        If ParserMK.LanguageSP = ELanguage.JavaScript Then

                            JavaScriptClass(self, tw)
                        Else
                            Select Case .KndCla
                                Case EClass.EnumCla
                                    '  列挙型の場合

                                    Select Case ParserMK.LanguageSP
                                        Case ELanguage.Basic
                                            tw.Fmt(EToken.Public_, EToken.Enum_, self, EToken.NL)

                                            For Each fld1 In .FldCla
                                                tw.TAB(1)
                                                tw.Fmt(fld1, EToken.NL)
                                            Next

                                            tw.Fmt(EToken.End_, EToken.Enum_, EToken.NL)

                                        Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                            tw.Fmt(EToken.Public_, EToken.Enum_, self, EToken.LC, EToken.NL)

                                            For Each fld1 In .FldCla
                                                tw.Fmt(fld1, EToken.Comma, EToken.NL)
                                            Next

                                            tw.Fmt(EToken.RC, EToken.NL)
                                    End Select

                                Case EClass.DelegateCla
                                    ' デリゲートの場合

                                    If TypeOf self Is TDelegate Then
                                        With CType(self, TDelegate)
                                            tw.Fmt(EToken.Public_, EToken.Delegate_)

                                            Select Case ParserMK.LanguageSP
                                                Case ELanguage.Basic
                                                    If .RetDlg Is Nothing Then
                                                        tw.Fmt(EToken.Sub_)
                                                    Else
                                                        tw.Fmt(EToken.Function_)
                                                    End If
                                                Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                            End Select

                                            tw.Fmt(.NameVar)

                                            tw.Fmt(EToken.LP)
                                            tw.Fmt(Laminate((From var1 In .ArgDlg Select var1.TokenListVar), New TToken(EToken.Comma, self)))
                                            tw.Fmt(EToken.RP)

                                            If .RetDlg IsNot Nothing Then
                                                PrjMK.SetClassNameList(.RetDlg, ParserMK)
                                                tw.Fmt(EToken.As_, .RetDlg.TokenListVar)
                                            End If

                                            tw.Fmt(EToken.EOL)
                                        End With
                                    End If
                                Case Else
                                    '  クラスの場合

                                    If .ModCla().isPartial Then
                                        tw.Fmt(EToken.Partial_)
                                    End If

                                    tw.Fmt(EToken.Public_)

                                    If .ModCla().isAbstract Then
                                        tw.Fmt(EToken.Abstract)
                                    End If

                                    Select Case .KndCla
                                        Case EClass.ClassCla
                                            tw.Fmt(EToken.Class_)
                                        Case EClass.StructCla
                                            tw.Fmt(EToken.Struct)
                                        Case EClass.InterfaceCla
                                            tw.Fmt(EToken.Interface_)
                                        Case Else
                                            Debug.Assert(False)
                                    End Select


                                    tw.Fmt(.TokenListVar)

                                    If ParserMK.LanguageSP = ELanguage.Basic Then
                                        tw.Fmt(EToken.NL)
                                    End If

                                    If .DirectSuperClassList.Count <> 0 AndAlso .DirectSuperClassList(0) IsNot PrjMK.ObjectType Then
                                        tw.Fmt(EToken.Extends, .DirectSuperClassList(0).TokenListVar)

                                        If ParserMK.LanguageSP = ELanguage.Basic Then
                                            tw.Fmt(EToken.NL)
                                        End If
                                    End If

                                    If .InterfaceList.Count <> 0 AndAlso .InterfaceList(0) IsNot PrjMK.ObjectType Then
                                        tw.Fmt(EToken.Implements_)
                                        tw.Fmt(Laminate((From cls1 In .InterfaceList Select cls1.TokenListVar), New TToken(EToken.Comma, self)))

                                        If ParserMK.LanguageSP = ELanguage.Basic Then
                                            tw.Fmt(EToken.NL)
                                        End If
                                    End If

                                    If ParserMK.LanguageSP <> ELanguage.Basic Then
                                        tw.Fmt(EToken.LC, EToken.NL)
                                    End If

                                    '  すべてのフィールドに対し
                                    For Each fld1 In .FldCla
                                        tw.TAB(1)
                                        tw.Fmt(fld1.TokenListVar)
                                    Next

                                    '  すべてのメソッドに対し
                                    For Each fnc1 In .FncCla
                                        If fnc1.IsTreeWalker OrElse fnc1.IsInitializer() Then
                                            gen_tw.Fmt(fnc1.TokenListVar)
                                        Else
                                            tw.Fmt(fnc1.TokenListVar)
                                        End If
                                    Next

                                    If ParserMK.LanguageSP = ELanguage.Basic Then
                                        Select Case .KndCla
                                            Case EClass.ClassCla
                                                tw.Fmt(EToken.EndClass, EToken.NL)
                                            Case EClass.StructCla
                                                tw.Fmt(EToken.EndStruct, EToken.NL)
                                            Case EClass.InterfaceCla
                                                tw.Fmt(EToken.EndInterface, EToken.NL)
                                        End Select
                                    Else
                                        tw.Fmt(EToken.RC, EToken.NL)
                                    End If
                            End Select
                        End If

                        .TokenListCls = tw.GetTokenList()

                        If gen_tw.TokenListTW.Count <> 0 Then
                            .GenTokenListCls = gen_tw.GetTokenList()
                        End If
                    End With

                ElseIf TypeOf self Is TFunction Then
                    With CType(self, TFunction)

                        If Not .IsInitializer() OrElse ParserMK.LanguageSP = ELanguage.JavaScript Then
                            If .ComVar IsNot Nothing Then

                                tw.Fmt(.ComVar.TokenList)
                            End If

                            tw.TAB(1)
                            If ParserMK.LanguageSP <> ELanguage.JavaScript Then

                                tw.Fmt(.ModVar.TokenListMod)
                            End If

                            Select Case ParserMK.LanguageSP
                                Case ELanguage.Basic
                                    Select Case .TypeFnc
                                        Case EToken.Function_
                                            tw.Fmt(EToken.Function_, self)
                                        Case EToken.Sub_
                                            tw.Fmt(EToken.Sub_, self)
                                        Case EToken.New_
                                            tw.Fmt(EToken.Sub_, EToken.New_)

                                        Case EToken.Operator_
                                            tw.Fmt(EToken.Operator_, self)

                                        Case Else
                                            Debug.WriteLine("")
                                    End Select

                                Case ELanguage.TypeScript
                                    Select Case .TypeFnc
                                        Case EToken.Function_, EToken.Sub_
                                            tw.Fmt(self)

                                        Case EToken.New_
                                            tw.Fmt(EToken.Constructor)

                                        Case EToken.Operator_
                                            tw.Fmt(EToken.Operator_, self)

                                        Case Else
                                            Debug.WriteLine("関数のタイプが不明:" + .NameVar)
                                    End Select

                                Case ELanguage.JavaScript
                                    Select Case .TypeFnc
                                        Case EToken.Function_, EToken.Sub_
                                            tw.Fmt(.ClaFnc.NameVar, EToken.Dot, "prototype", EToken.Dot, .NameVar, EToken.ASN, EToken.Function_)

                                        Case EToken.New_
                                            tw.Fmt(EToken.Var, .ClaFnc.NameVar, EToken.ASN, EToken.Function_)
                                        Case Else
                                            Debug.Assert(False)
                                    End Select

                                Case ELanguage.CSharp, ELanguage.Java
                            End Select

                            tw.Fmt(EToken.LP)
                            tw.Fmt(Laminate((From var1 In .ArgFnc Select var1.TokenListVar), New TToken(EToken.Comma, self)))
                            tw.Fmt(EToken.RP)

                            If ParserMK.LanguageSP <> ELanguage.JavaScript Then

                                If .RetType IsNot Nothing Then
                                    PrjMK.SetClassNameList(.RetType, ParserMK)
                                    tw.Fmt(EToken.As_, .RetType.TokenListVar)

                                End If
                            End If

                            If .InterfaceFnc IsNot Nothing Then
                                PrjMK.SetClassNameList(.InterfaceFnc, ParserMK)
                                tw.Fmt(EToken.Implements_, .InterfaceFnc.TokenListVar, EToken.Dot, .ImplFnc)
                            End If

                            If ParserMK.LanguageSP = ELanguage.Basic Then

                                tw.Fmt(EToken.NL)
                                If .BlcFnc IsNot Nothing Then

                                    If .WithFnc IsNot Nothing Then
                                        If .WithFnc Is .ArgFnc(0).TypeVar Then
                                            tw.Fmt(EToken.With_, .ArgFnc(0).NameVar, EToken.NL)
                                        Else
                                            tw.TAB(2)
                                            tw.Fmt(EToken.With_, EToken.CType_, EToken.LP, .ArgFnc(0).NameVar, EToken.Comma, .WithFnc.TokenListVar, EToken.RP, EToken.NL)
                                        End If
                                        tw.Fmt(.BlcFnc.TokenList)

                                        tw.TAB(2)
                                        tw.Fmt(EToken.EndWith, EToken.NL)
                                    Else
                                        tw.Fmt(.BlcFnc.TokenList)
                                    End If

                                    tw.TAB(1)
                                    Select Case .TypeFnc
                                        Case EToken.Operator_
                                            tw.Fmt(EToken.EndOperator)
                                        Case EToken.Sub_, EToken.New_
                                            tw.Fmt(EToken.EndSub)
                                        Case EToken.Function_
                                            tw.Fmt(EToken.EndFunction)
                                    End Select

                                    tw.Fmt(EToken.NL)
                                End If
                            Else

                                If .BlcFnc Is Nothing Then
                                    tw.Fmt(EToken.EOL)
                                Else
                                    tw.Fmt(EToken.LC, EToken.NL)

                                    If ParserMK.LanguageSP = ELanguage.JavaScript AndAlso .TypeFnc = EToken.New_ Then
                                        ' JavaScriptのコンストラクタの場合

                                        ' InstanceInitializerを再帰的に呼ぶ。CallInstanceInitializer(this, this);
                                        tw.Fmt("CallInstanceInitializer", EToken.LP, ParserMK.ThisName, EToken.Comma, ParserMK.ThisName, EToken.RP, EToken.EOL)
                                    End If

                                    tw.Fmt(.BlcFnc.TokenList)

                                    tw.Fmt(EToken.RC, EToken.NL)
                                End If
                            End If

                        End If

                        If ParserMK.LanguageSP = ELanguage.JavaScript AndAlso .TypeFnc = EToken.New_ Then
                            ' JavaScriptのコンストラクタの場合

                            If .ClaFnc.DirectSuperClassList.Count <> 0 AndAlso .ClaFnc.DirectSuperClassList(0) IsNot PrjMK.ObjectType Then
                                ' 親クラスがObjectでない場合

                                ' 親クラスを継承する。
                                tw.Fmt("Inherits", EToken.LP, .ClaFnc.NameVar, EToken.Comma, .ClaFnc.DirectSuperClassList(0).NameVar, EToken.RP, EToken.EOL)
                            End If
                        End If

                        .TokenListVar = tw.GetTokenList()
                    End With

                ElseIf TypeOf self Is TField Then
                    With CType(self, TField)

                        If .ComVar IsNot Nothing Then
                            tw.Fmt(.ComVar.TokenList)
                        End If

                        Select Case ParserMK.LanguageSP
                            Case ELanguage.Basic
                                If .ModVar IsNot Nothing Then
                                    tw.Fmt(.ModVar.TokenListMod)
                                End If
                                If .ModVar Is Nothing OrElse Not .ModVar.isPublic AndAlso Not .ModVar.isShared Then
                                    tw.Fmt(EToken.Var)
                                End If

                            Case ELanguage.TypeScript

                            Case ELanguage.JavaScript
                                tw.Fmt(EToken.Var)

                            Case ELanguage.CSharp, ELanguage.Java
                        End Select

                        tw.Fmt(self)
                        VariableTypeInitializer(self, tw)

                        If .TailCom <> "" Then
                            tw.Fmt(New TToken(EToken.Comment, .TailCom))
                        End If

                        tw.Fmt(EToken.EOL)

                        .TokenListVar = tw.GetTokenList()
                    End With

                Else

                    If False AndAlso ParserMK.LanguageSP = ELanguage.JavaScript Then
                        tw.Fmt(self)
                    Else
                        If .ByRefVar Then
                            tw.Fmt(EToken.Ref)
                        End If
                        If .ParamArrayVar Then
                            tw.Fmt(EToken.ParamArray_)
                        End If

                        tw.Fmt(self)
                        VariableTypeInitializer(self, tw)
                    End If

                    .TokenListVar = tw.GetTokenList()
                End If

            End With
        End If

        If TypeOf self Is TStatement Then
            With CType(self, TStatement)

                If .BeforeSrc IsNot Nothing Then
                    Dim v = .BeforeSrc.Replace(vbCr, "").Split(New Char() {vbLf(0)})
                    For Each s In v
                        tw.Fmt(s, EToken.NL)
                    Next
                End If

                If .ComStmt IsNot Nothing Then
                    For Each tkn_f In .ComStmt
                        tw.TAB(.TabStmt)
                        tw.Fmt(tkn_f.StrTkn, EToken.NL)
                    Next
                End If
                If .IsGenerated Then

                ElseIf TypeOf self Is TAssignment OrElse TypeOf self Is TCall OrElse TypeOf self Is TVariableDeclaration Then
                    tw.TAB(.TabStmt)
                    If TypeOf self Is TAssignment Then
                        With CType(self, TAssignment)
                            Dim asn_op As EToken

                            If ParserMK.LanguageSP <> ELanguage.Basic AndAlso .RelAsn.TypeApp = EToken.Eq Then
                                asn_op = EToken.ASN
                            Else
                                asn_op = .RelAsn.TypeApp
                            End If

                            tw.Fmt(.RelAsn.ArgApp(0).TokenList, asn_op, .RelAsn.ArgApp(1).TokenList)
                        End With

                    ElseIf TypeOf self Is TCall Then
                        With CType(self, TCall)
                            tw.Fmt(.AppCall.TokenList)
                        End With

                    ElseIf TypeOf self Is TVariableDeclaration Then
                        With CType(self, TVariableDeclaration)

                            tw.Fmt(.ModDecl.TokenListMod)
                            If .ModDecl Is Nothing OrElse Not .ModDecl.isPublic AndAlso Not .ModDecl.isShared Then
                                tw.Fmt(EToken.Var)
                            End If

                            tw.Fmt(Laminate((From var1 In .VarDecl Select var1.TokenListVar), New TToken(EToken.Comma, self)))

                        End With
                    End If

                    If .TailCom <> "" Then

                        tw.Fmt(New TToken(EToken.Comment, .TailCom))
                    End If
                    tw.Fmt(EToken.EOL)

                ElseIf TypeOf self Is TIfBlock Then
                    With CType(self, TIfBlock)
                        Dim if1 As TIf, i1 As Integer

                        if1 = CType(.UpTrm, TIf)
                        i1 = if1.IfBlc.IndexOf(CType(self, TIfBlock))
                        Debug.Assert(i1 <> -1)

                        tw.TAB(.TabStmt)
                        Select Case ParserMK.LanguageSP
                            Case ELanguage.Basic
                                If i1 = 0 Then
                                    tw.Fmt(EToken.If_, .CndIf.TokenList, EToken.Then_, EToken.NL)
                                Else
                                    If .CndIf IsNot Nothing Then
                                        tw.Fmt(EToken.ElseIf_, .CndIf.TokenList, EToken.Then_, EToken.NL)
                                    Else
                                        tw.Fmt(EToken.Else_, EToken.NL)
                                    End If
                                End If

                            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                If i1 = 0 Then
                                    tw.Fmt(EToken.If_, EToken.LP, .CndIf.TokenList, EToken.RP, EToken.LC, EToken.NL)
                                Else
                                    If .CndIf IsNot Nothing Then
                                        tw.Fmt(EToken.Else_, EToken.If_, EToken.LP, .CndIf.TokenList, EToken.RP, EToken.LC, EToken.NL)
                                    Else
                                        tw.Fmt(EToken.Else_, EToken.LC, EToken.NL)
                                    End If
                                End If
                        End Select

                        If ParserMK.LanguageSP = ELanguage.Basic AndAlso .WithIf IsNot Nothing Then
                            tw.TAB(.TabStmt + 1)
                            tw.Fmt(EToken.With_, .WithIf.TokenList, EToken.NL)

                            tw.Fmt(.BlcIf.TokenList)

                            tw.TAB(.TabStmt + 1)
                            tw.Fmt(EToken.EndWith, EToken.NL)
                        Else
                            tw.Fmt(.BlcIf.TokenList)
                        End If


                        If ParserMK.LanguageSP <> ELanguage.Basic Then
                            tw.TAB(.TabStmt)
                            tw.Fmt(EToken.RC, EToken.NL)
                        End If

                    End With

                ElseIf TypeOf self Is TIf Then
                    With CType(self, TIf)
                        For Each if_blc In .IfBlc
                            tw.Fmt(if_blc.TokenList)
                        Next

                        If ParserMK.LanguageSP = ELanguage.Basic Then
                            tw.TAB(.TabStmt)
                            tw.Fmt(EToken.EndIf_, EToken.NL)
                        End If
                    End With

                ElseIf TypeOf self Is TCase Then
                    With CType(self, TCase)
                        tw.TAB(.TabStmt)

                        Select Case ParserMK.LanguageSP
                            Case ELanguage.Basic
                                If Not .DefaultCase Then
                                    tw.Fmt(EToken.Case_, Laminate((From trm In .TrmCase Select trm.TokenList), New TToken(EToken.Comma, self)), EToken.NL)
                                Else
                                    tw.Fmt(EToken.Case_, EToken.Else_, EToken.NL)
                                End If

                                tw.Fmt(.BlcCase.TokenList)

                            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                If Not .DefaultCase Then
                                    tw.Fmt(EToken.Case_, Laminate((From trm In .TrmCase Select trm.TokenList), New TToken(EToken.Comma, self)), EToken.MMB, EToken.NL)
                                Else
                                    tw.Fmt(EToken.Default_, EToken.NL)
                                End If

                                tw.Fmt(.BlcCase.TokenList)

                                tw.Fmt(EToken.Break_, EToken.EOL)
                        End Select

                    End With

                ElseIf TypeOf self Is TSelect Then
                    With CType(self, TSelect)
                        tw.TAB(.TabStmt)
                        Select Case ParserMK.LanguageSP
                            Case ELanguage.Basic
                                tw.Fmt(EToken.Select_, EToken.Case_, .TrmSel.TokenList, EToken.NL)

                            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                tw.Fmt(EToken.Switch, EToken.LP, .TrmSel.TokenList, EToken.RP, EToken.LC, EToken.NL)
                        End Select


                        For Each cas1 In .CaseSel
                            tw.Fmt(cas1.TokenList)
                        Next

                        tw.TAB(.TabStmt)
                        Select Case ParserMK.LanguageSP
                            Case ELanguage.Basic
                                tw.Fmt(EToken.EndSelect, EToken.NL)
                            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                tw.Fmt(EToken.RC, EToken.NL)
                        End Select
                    End With

                ElseIf TypeOf self Is TTry Then
                    With CType(self, TTry)

                        Select Case ParserMK.LanguageSP
                            Case ELanguage.Basic
                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.Try_, EToken.NL)

                                tw.Fmt(.BlcTry.TokenList)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.Catch_, Laminate((From var1 In .VarCatch Select var1.TokenListVar), New TToken(EToken.Comma, self)), EToken.NL)

                                tw.Fmt(.BlcCatch.TokenList)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.EndTry, EToken.NL)

                            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.Try_, EToken.LC, EToken.NL)

                                tw.Fmt(.BlcTry.TokenList)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.RC, EToken.NL)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.Catch_, EToken.LP, Laminate((From var1 In .VarCatch Select var1.TokenListVar), New TToken(EToken.Comma, self)), EToken.RP, EToken.LC, EToken.NL)

                                tw.Fmt(.BlcCatch.TokenList)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.RC, EToken.NL)
                        End Select

                    End With

                ElseIf TypeOf self Is TFor Then
                    With CType(self, TFor)
                        If .IsDo Then

                            If ParserMK.LanguageSP = ELanguage.Basic Then
                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.Do_, EToken.While_, .CndFor.TokenList, EToken.NL)

                                tw.Fmt(.BlcFor.TokenList)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.Loop_, EToken.NL)
                            Else
                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.While_, EToken.LP, .CndFor.TokenList, EToken.RP, EToken.LC, EToken.NL)

                                tw.Fmt(.BlcFor.TokenList)

                                tw.TAB(.TabStmt)
                                tw.Fmt(EToken.RC, EToken.NL)
                            End If


                        ElseIf .InVarFor IsNot Nothing Then
                            Select Case ParserMK.LanguageSP
                                Case ELanguage.Basic
                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.For_, EToken.Each_, .InVarFor.NameVar, EToken.In_, .InTrmFor.TokenList, EToken.NL)

                                    tw.Fmt(.BlcFor.TokenList)

                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.Next_, EToken.NL)

                                Case ELanguage.TypeScript, ELanguage.CSharp, ELanguage.Java
                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.For_, EToken.LP, EToken.Var, .InVarFor.NameVar, EToken.In_, .InTrmFor.TokenList, EToken.RP, EToken.LC, EToken.NL)

                                    tw.Fmt(.BlcFor.TokenList)

                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.RC, EToken.NL)

                                Case ELanguage.JavaScript
                                    ' for(var $i = 0; $i < v.length; $i++)
                                    ' var x = v[$i];
                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.For_, EToken.LP, EToken.Var, "$i", EToken.ASN, 0, EToken.SM, "$i", EToken.LT, .InTrmFor.TokenList, EToken.Dot, "length", EToken.SM, "$i++", EToken.RP, EToken.LC, EToken.NL)
                                    tw.Fmt(EToken.Var, .InVarFor.NameVar, EToken.ASN, .InTrmFor.TokenList, EToken.LB, "$i", EToken.RB, EToken.EOL)

                                    tw.Fmt(.BlcFor.TokenList)

                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.RC, EToken.NL)
                            End Select

                        ElseIf .IdxVarFor IsNot Nothing Then
                            Select Case ParserMK.LanguageSP
                                Case ELanguage.Basic

                                Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.CSharp, ELanguage.Java
                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.For_, EToken.LP, EToken.Var, .IdxVarFor, EToken.SM, .CndFor.TokenList, EToken.SM, .StepStmtFor.TokenList, EToken.RP, EToken.LC, EToken.NL)

                                    tw.Fmt(.BlcFor.TokenList)

                                    tw.TAB(.TabStmt)
                                    tw.Fmt(EToken.RC, EToken.NL)
                            End Select

                        ElseIf .FromFor IsNot Nothing Then
                            tw.TAB(.TabStmt)
                            tw.Fmt(EToken.For_, .IdxFor.TokenList, EToken.Eq, .FromFor.TokenList, EToken.To_, .ToFor.TokenList)

                            If .StepFor IsNot Nothing Then
                                tw.Fmt(EToken.Step_, .StepFor.TokenList)
                            End If
                            tw.Fmt(EToken.NL)

                            tw.Fmt(.BlcFor.TokenList)

                            tw.TAB(.TabStmt)
                            tw.Fmt(EToken.Next_, EToken.NL)
                        Else
                            Debug.Assert(False, "For Src Bas")
                        End If
                    End With

                ElseIf TypeOf self Is TReDim Then
                    With CType(self, TReDim)
                        tw.TAB(.TabStmt)
                        tw.Fmt(EToken.ReDim_, .TrmReDim.TokenList, EToken.LP, Laminate((From trm In .DimReDim Select trm.TokenList), New TToken(EToken.Comma, self)), EToken.RP, EToken.NL)
                    End With

                ElseIf TypeOf self Is TBlock Then
                    With CType(self, TBlock)
                        For Each stmt In .StmtBlc
                            tw.Fmt(stmt.TokenList)
                        Next
                    End With

                ElseIf TypeOf self Is TReturn Then
                    With CType(self, TReturn)
                        tw.TAB(.TabStmt)
                        If .YieldRet Then
                            tw.Fmt(EToken.Yield_)
                        Else
                            tw.Fmt(EToken.Return_)
                        End If
                        If .TrmRet IsNot Nothing Then
                            tw.Fmt(.TrmRet.TokenList)
                        End If
                        tw.Fmt(EToken.EOL)

                    End With

                ElseIf TypeOf self Is TThrow Then
                    With CType(self, TThrow)
                        tw.TAB(.TabStmt)
                        tw.Fmt(EToken.Throw_, .TrmThrow.TokenList, EToken.EOL)
                    End With

                ElseIf TypeOf self Is TComment Then
                    With CType(self, TComment)
                        ComSrc(CType(self, TComment), .TabStmt, tw)
                    End With

                Else
                    Select Case .TypeStmt
                        Case EToken.ExitDo, EToken.ExitFor, EToken.ExitSub
                            tw.TAB(.TabStmt)
                            Select Case .TypeStmt
                                Case EToken.ExitDo
                                    tw.Fmt(EToken.ExitDo, EToken.NL)
                                Case EToken.ExitFor
                                    tw.Fmt(EToken.ExitFor, EToken.NL)
                                Case EToken.ExitSub
                                    tw.Fmt(EToken.ExitSub, EToken.NL)
                                Case Else
                                    Debug.Assert(False)
                            End Select

                        Case Else
                            Debug.WriteLine("Err Stmt Src:{0}", self)
                            Debug.Assert(False)
                    End Select
                End If

                If .AfterSrc <> "" Then
                    Dim v = .AfterSrc.Trim().Replace(vbCr, "").Split(New Char() {vbLf(0)})
                    For Each s In v
                        tw.Fmt(s, EToken.NL)
                    Next
                End If

                .TokenList = tw.TokenListTW
            End With

        ElseIf TypeOf self Is TTerm Then
            With CType(self, TTerm)

                If .CastType IsNot Nothing AndAlso ParserMK.LanguageSP <> ELanguage.JavaScript Then

                    tw.Fmt(EToken.CType_, EToken.LP)
                End If

                If TypeOf self Is TConstant Then
                    With CType(self, TConstant)
                        Select Case .TypeAtm
                            Case EToken.Char_
                                tw.Fmt(New TToken(.TypeAtm, """" + Escape(.NameRef) + """c"))

                            Case EToken.String_
                                tw.Fmt(New TToken(.TypeAtm, """" + Escape(.NameRef) + """"))

                            Case EToken.RegEx
                                tw.Fmt(Escape(.NameRef))

                            Case EToken.Int
                                tw.Fmt(.NameRef)

                            Case EToken.Hex
                                tw.Fmt(.NameRef)
                                Debug.Assert(TSys.Substring(.NameRef, 0, 2) = "&H")

                            Case Else
                                Debug.Assert(False)
                        End Select

                    End With

                ElseIf TypeOf self Is TArray Then
                    With CType(self, TArray)
                        tw.Fmt(EToken.LC, Laminate((From trm In .TrmArr Select trm.TokenList), New TToken(EToken.Comma, self)), EToken.RC)
                    End With

                ElseIf TypeOf self Is TDot Then
                    With CType(self, TDot)
                        If .IsAddressOf Then
                            tw.Fmt(EToken.AddressOf_)
                        End If

                        If .TrmDot Is Nothing Then
                            If ParserMK.LanguageSP = ELanguage.JavaScript Then

                                tw.Fmt(.FunctionTrm.ArgFnc(0).NameVar)
                            End If
                        Else
                            tw.Fmt(.TrmDot.TokenList)
                        End If

                        tw.Fmt(EToken.Dot, ParserMK.TranslageReferenceName(CType(self, TDot)))
                    End With

                ElseIf TypeOf self Is TReference Then
                    With CType(self, TReference)
                        If .IsAddressOf Then
                            tw.Fmt(EToken.AddressOf_)
                        End If

                        If ParserMK.LanguageSP = ELanguage.JavaScript Then
                            ' JavaScriptの場合

                            If .NameRef = "Nothing" Then

                                tw.Fmt("undefined")

                            ElseIf TypeOf .VarRef Is TField AndAlso CType(.VarRef, TField).ClaFld IsNot ParserMK.PrjParse.SystemType OrElse TypeOf .VarRef Is TFunction AndAlso CType(.VarRef, TFunction).ClaFnc IsNot ParserMK.PrjParse.SystemType Then
                                ' Systemクラス以外のフィールドの参照の場合

                                If ParserMK.LanguageSP = ELanguage.JavaScript AndAlso TypeOf .UpTrm Is TApply AndAlso CType(.UpTrm, TApply).TypeApp = EToken.BaseCall Then
                                    tw.Fmt(self)
                                Else
                                    tw.Fmt(ParserMK.ThisName, EToken.Dot, self)
                                End If
                            Else

                                tw.Fmt(self)
                            End If

                        Else
                            tw.Fmt(self)
                        End If

                    End With

                ElseIf TypeOf self Is TApply Then
                    With CType(self, TApply)

                        If .Negation Then

                            tw.Fmt(EToken.Not_)
                        End If

                        Select Case .TypeApp
                            Case EToken.OR_, EToken.And_, EToken.Anp, EToken.BitOR
                                tw.Fmt(Laminate((From trm In .ArgApp Select trm.TokenList), New TToken(.TypeApp, self)))
                            Case EToken.Not_
                                tw.Fmt(EToken.Not_, .ArgApp(0).TokenList)

                            '--------------------------------------------------------------------------------------
                            Case EToken.ADD, EToken.Mns, EToken.MUL, EToken.DIV, EToken.MOD_, EToken.BitOR
                                If .ArgApp.Count = 1 AndAlso (.TypeApp = EToken.ADD OrElse .TypeApp = EToken.Mns) Then
                                    tw.Fmt(.TypeApp, .ArgApp(0).TokenList)
                                Else

                                    tw.Fmt(.ArgApp(0).TokenList, .TypeApp, .ArgApp(1).TokenList)
                                End If

                            Case EToken.INC, EToken.DEC
                                tw.Fmt(.ArgApp(0).TokenList, .TypeApp)

                            Case EToken.AppCall
                                tw.Fmt(.FncApp.TokenList, AppArgTokenList(self))

                            Case EToken.BaseCall

                                If ParserMK.LanguageSP = ELanguage.JavaScript Then
                                    tw.Fmt("this", EToken.Dot, "SuperClass", EToken.Dot, .FncApp.TokenList, AppArgTokenList(self))
                                Else
                                    tw.Fmt(EToken.Base, EToken.Dot, .FncApp.TokenList, AppArgTokenList(self))
                                End If

                            Case EToken.BaseNew

                                tw.Fmt(EToken.Base, EToken.Dot, EToken.New_, AppArgTokenList(self))

                            Case EToken.New_
                                Debug.Assert(.NewApp IsNot Nothing)

                                If ParserMK.LanguageSP = ELanguage.JavaScript AndAlso .NewApp.OrgCla IsNot Nothing AndAlso .NewApp.OrgCla.NameVar = "TList" Then
                                    ' IEnumerableから作るリストは未対応
                                    Debug.Assert(.ArgApp.Count = 0)
                                    tw.Fmt(EToken.LB, EToken.RB)
                                Else

                                    tw.Fmt(EToken.New_)

                                    If .IniApp Is Nothing Then
                                        ' 初期値がない場合

                                        tw.Fmt(.NewApp.TokenListVar, AppArgTokenList(self))
                                    Else
                                        ' 初期値がある場合

                                        If .NewApp.IsArray() Then
                                            ' 配列の場合

                                            tw.Fmt(.NewApp.GenCla(0).TokenListVar, AppArgTokenList(self))
                                        Else
                                            ' 配列でない場合

                                            tw.Fmt(.NewApp.TokenListVar, AppArgTokenList(self), EToken.From_)
                                        End If

                                        tw.Fmt(.IniApp.TokenList)
                                    End If
                                End If


                            Case EToken.As_, EToken.Cast

                                tw.Fmt(EToken.CType_, EToken.LP, .ArgApp(0).TokenList, EToken.Comma, .ClassApp.TokenListVar, EToken.RP)

                            Case EToken.GetType_
                                tw.Fmt(EToken.GetType_, EToken.LP, .ClassApp.TokenListVar, EToken.RP)

                            Case EToken.Question
                                tw.Fmt(EToken.If_, EToken.LP, .ArgApp(0).TokenList, EToken.Comma, .ArgApp(1).TokenList, EToken.Comma, .ArgApp(2).TokenList, EToken.RP)

                            Case EToken.Instanceof
                                Dim test_class As TClass = CType(CType(.ArgApp(1), TReference).VarRef, TClass)
                                If ParserMK.LanguageSP = ELanguage.Basic Then

                                    tw.Fmt(EToken.Instanceof, .ArgApp(0).TokenList, EToken.Is_, test_class.TokenListVar)
                                Else
                                    If test_class.OrgCla IsNot Nothing Then
                                        tw.Fmt("Array", EToken.Dot, "isArray", EToken.LP, .ArgApp(0).TokenList, EToken.RP)
                                    Else
                                        tw.Fmt(EToken.LP, .ArgApp(0).TokenList, EToken.Instanceof, test_class.TokenListVar, EToken.RP)
                                    End If
                                End If

                            '--------------------------------------------------------------------------------------
                            Case EToken.Eq, EToken.NE
                                Dim tp1 As TClass, tp2 As TClass

                                tw.Fmt(.ArgApp(0).TokenList)
                                tp1 = .ArgApp(0).TypeTrm
                                tp2 = .ArgApp(1).TypeTrm
                                If tp1 Is Nothing OrElse tp2 Is Nothing Then
                                    ' Debug.WriteLine("");
                                    ' tp1 = .ArgApp[0].TypeTrm;
                                    ' tp2 = .ArgApp[1].TypeTrm;
                                End If
                                If ParserMK.LanguageSP <> ELanguage.Basic OrElse tp1 IsNot Nothing AndAlso (tp1.IsAtomType() OrElse tp1.KndCla = EClass.StructCla) OrElse tp2 IsNot Nothing AndAlso (tp2.IsAtomType() OrElse tp2.KndCla = EClass.StructCla) Then
                                    tw.Fmt(.TypeApp)
                                Else
                                    If .TypeApp = EToken.NE Then
                                        tw.Fmt(EToken.IsNot_)
                                    Else
                                        tw.Fmt(EToken.Is_)
                                    End If
                                End If
                                tw.Fmt(.ArgApp(1).TokenList)
                            Case EToken.ASN, EToken.LT, EToken.GT, EToken.ADDEQ, EToken.SUBEQ, EToken.MULEQ, EToken.DIVEQ, EToken.MODEQ, EToken.LE, EToken.GE
                                tw.Fmt(.ArgApp(0).TokenList, .TypeApp, .ArgApp(1).TokenList)
                            Case EToken.IsNot_
                                tw.Fmt(.ArgApp(0).TokenList, EToken.IsNot_, .ArgApp(1).TokenList)

                            Case EToken.Instanceof
                                If ParserMK.LanguageSP = ELanguage.Basic Then
                                    tw.Fmt(.ArgApp(0).TokenList, .TypeApp, .ArgApp(1).TokenList)
                                Else
                                    tw.Fmt(EToken.Instanceof, .ArgApp(0).TokenList, EToken.Is_, .ArgApp(1).TokenList)
                                End If

                            Case EToken.Is_
                                tw.Fmt(.ArgApp(0).TokenList, EToken.Is_, .ArgApp(1).TokenList)

                            Case Else
                                Debug.Assert(False)
                        End Select
                    End With

                ElseIf TypeOf self Is TParenthesis Then
                    With CType(self, TParenthesis)
                        If .TrmPar.IsApp() AndAlso CType(.TrmPar, TApply).TypeApp = EToken.Cast Then

                            tw.Fmt(.TrmPar.TokenList)
                        Else

                            tw.Fmt(EToken.LP, .TrmPar.TokenList, EToken.RP)
                        End If

                    End With
                ElseIf TypeOf self Is TFrom Then
                    With CType(self, TFrom)
                        If ParserMK.LanguageSP = ELanguage.JavaScript Then
                            '            var vx = $Query(self.Children, function (x) { return true; }, function (x) { return x.BoundingRectangle.Position.X });
                            tw.Fmt("$Query(", .SeqQry.TokenList)

                            If .CndQry IsNot Nothing Then

                                tw.Fmt(", function(", .VarQry.NameVar, "){ return ", .CndQry.TokenList, "; }")
                            Else
                                tw.Fmt(", undefined")
                            End If

                            If .SelFrom IsNot Nothing Then

                                tw.Fmt(", function(", .VarQry.NameVar, "){ return ", .SelFrom.TokenList, "; }")
                            Else
                                tw.Fmt(", undefined")
                            End If

                            tw.Fmt(")")
                        Else
                            tw.Fmt(EToken.From_, .VarQry.NameVar, EToken.In_, .SeqQry.TokenList)

                            If .CndQry IsNot Nothing Then

                                tw.Fmt(EToken.Where_, .CndQry.TokenList)
                            End If

                            If .SelFrom IsNot Nothing Then

                                tw.Fmt(EToken.Select_, .SelFrom.TokenList)
                            End If

                            If .TakeFrom IsNot Nothing Then

                                tw.Fmt(EToken.Take_, .TakeFrom.TokenList)
                            End If

                            If .InnerFrom IsNot Nothing Then

                                tw.Fmt(.InnerFrom.TokenList)
                            End If
                        End If
                    End With

                ElseIf TypeOf self Is TAggregate Then
                    With CType(self, TAggregate)
                        If ParserMK.LanguageSP = ELanguage.JavaScript Then
                            '            var vx = $Query(self.Children, function (x) { return true; }, function (x) { return x.BoundingRectangle.Position.X });
                            tw.Fmt("$Query(", .SeqQry.TokenList)

                            If .CndQry IsNot Nothing Then

                                tw.Fmt(", function(", .VarQry.NameVar, "){ return ", .CndQry.TokenList, "; }")
                            Else
                                tw.Fmt(", undefined")
                            End If

                            tw.Fmt(")")

                            Select Case .FunctionAggr
                                Case EAggregateFunction.Sum
                                    tw.Fmt(".Sum()")
                                Case EAggregateFunction.Max
                                    tw.Fmt(".Max()")
                                Case EAggregateFunction.Min
                                    tw.Fmt(".Min()")
                                Case EAggregateFunction.Average
                                    tw.Fmt(".Average()")
                                Case Else
                                    Debug.Assert(False)
                            End Select

                        Else
                            tw.Fmt(EToken.Aggregate_, .VarQry.NameVar, EToken.In_, .SeqQry.TokenList)

                            If .CndQry IsNot Nothing Then

                                tw.Fmt(EToken.Where_, .CndQry.TokenList)
                            End If

                            tw.Fmt(EToken.Into_)

                            Select Case .FunctionAggr
                                Case EAggregateFunction.Sum
                                    tw.Fmt("Sum")
                                Case EAggregateFunction.Max
                                    tw.Fmt("Max")
                                Case EAggregateFunction.Min
                                    tw.Fmt("Min")
                                Case EAggregateFunction.Average
                                    tw.Fmt("Average")
                                Case Else
                                    Debug.Assert(False)
                            End Select
                            tw.Fmt(EToken.LP, .IntoAggr.TokenList, EToken.RP)
                        End If

                    End With
                Else
                    Debug.Assert(False)
                End If

                If .CastType IsNot Nothing AndAlso ParserMK.LanguageSP <> ELanguage.JavaScript Then

                    tw.Fmt(EToken.Comma, .CastType.TokenListVar, EToken.RP)
                End If

                .TokenList = tw.GetTokenList()
            End With

        ElseIf TypeOf self Is TModifier Then
            With CType(self, TModifier)
                If .isXmlIgnore OrElse .isWeak OrElse .isParent OrElse .isPrev OrElse .isNext OrElse .isInvariant Then
                    tw.Fmt(EToken.LT)

                    Dim need_comma As Boolean = False
                    If .isXmlIgnore Then
                        tw.Fmt("XmlIgnoreAttribute", EToken.LP, EToken.RP)
                        need_comma = True
                    End If

                    If .isWeak Then
                        If need_comma Then
                            tw.Fmt(EToken.Comma)
                        End If

                        tw.Fmt("_Weak", EToken.LP, EToken.RP)
                        need_comma = True
                    End If

                    If .isParent Then
                        If need_comma Then
                            tw.Fmt(EToken.Comma)
                        End If

                        tw.Fmt("_Parent", EToken.LP, EToken.RP)
                        need_comma = True
                    End If

                    If .isPrev Then
                        If need_comma Then
                            tw.Fmt(EToken.Comma)
                        End If

                        tw.Fmt("_Prev", EToken.LP, EToken.RP)
                        need_comma = True
                    End If

                    If .isNext Then
                        If need_comma Then
                            tw.Fmt(EToken.Comma)
                        End If

                        tw.Fmt("_Next", EToken.LP, EToken.RP)
                        need_comma = True
                    End If

                    If .isInvariant Then
                        tw.Fmt("_Invariant", EToken.LP, EToken.RP)
                    End If

                    tw.Fmt(EToken.GT)
                End If
                If .isPublic Then
                    tw.Fmt(EToken.Public_)
                End If
                If .isShared Then
                    tw.Fmt(EToken.Shared_)
                End If
                If .isIterator Then
                    tw.Fmt(EToken.Iterator_)
                End If
                If .isConst Then
                    tw.Fmt(EToken.Const_)
                End If
                If .isVirtual Then
                    tw.Fmt(EToken.Virtual)
                End If
                If .isMustOverride Then
                    tw.Fmt(EToken.MustOverride_)
                End If
                If .isOverride Then
                    tw.Fmt(EToken.Override)
                End If

                .TokenListMod = tw.GetTokenList()
            End With

        ElseIf TypeOf self Is TSourceFile Then
            With CType(self, TSourceFile)

                Select Case ParserMK.LanguageSP
                    Case ELanguage.Basic, ELanguage.CSharp
                        For Each str_f In .vUsing
                            tw.Fmt(EToken.Imports_, str_f, EToken.NL)
                        Next
                End Select

                For Each cla1 In .ClaSrc
                    'If PrjMK.OutputNotUsed OrElse cla1.UsedVar OrElse cla1.KndCla = EClass.DelegateCla Then

                    'End If
                    If cla1.TokenListCls Is Nothing Then
                        tw.Fmt(cla1.TokenListVar)
                    Else
                        tw.Fmt(cla1.TokenListCls)
                    End If
                Next

                .TokenListSrc = tw.GetTokenList()
            End With

        End If

    End Sub

    '   エスケープ文字を作る
    Public Function Escape(str1 As String) As String
        Dim sb As New TStringWriter

        For Each ch1 In str1
            If ch1 = """"c Then
                sb.Append("""""")
            Else
                sb.Append(ch1)
            End If
        Next

        Return sb.ToString()
    End Function

    Public Function Laminate(vvtkn As IEnumerable(Of List(Of TToken)), sep As TToken) As List(Of TToken)
        Dim i As Integer = 0
        Dim vtkn As New List(Of TToken)

        For Each tkns In vvtkn
            If i <> 0 Then
                vtkn.Add(sep)
            End If

            vtkn.AddRange(tkns)
            i += 1
        Next

        Return vtkn
    End Function

    Public Function AppArgTokenList(self As Object) As List(Of TToken)
        If TypeOf self Is TApply Then
            With CType(self, TApply)
                Dim vtkn As New List(Of TToken)
                Dim is_list As Boolean = False

                If .TypeApp = EToken.AppCall Then

                    Select Case .KndApp
                        Case EApply.CallApp, EApply.DictionaryApp
                        Case EApply.ArrayApp, EApply.StringApp, EApply.ListApp
                            is_list = True
                        Case Else
                            Debug.Assert(False)
                    End Select
                End If

                If is_list AndAlso ParserMK.LanguageSP <> ELanguage.Basic Then
                    vtkn.Add(New TToken(EToken.LB, self))
                    vtkn.AddRange(Laminate((From trm In .ArgApp Select trm.TokenList), New TToken(EToken.Comma, self)))
                    vtkn.Add(New TToken(EToken.RB, self))
                Else
                    vtkn.Add(New TToken(EToken.LP, self))
                    vtkn.AddRange(Laminate((From trm In .ArgApp Select trm.TokenList), New TToken(EToken.Comma, self)))
                    vtkn.Add(New TToken(EToken.RP, self))
                End If

                Return vtkn
            End With
        End If
        Debug.Assert(False)
        Return Nothing
    End Function

    Public Sub ModifierSrc(mod1 As TModifier)
        Dim tw As New TTokenWriter(Nothing, ParserMK)

        If mod1 IsNot Nothing Then
            If mod1.isPublic Then
                tw.Fmt(EToken.Public_)
            End If
            If mod1.isShared Then
                tw.Fmt(EToken.Shared_)
            End If
            If mod1.isConst Then
                tw.Fmt(EToken.Const_)
            End If
            If mod1.isVirtual Then
                tw.Fmt(EToken.Virtual)
            End If
            If mod1.isMustOverride Then
                tw.Fmt(EToken.MustOverride_)
            End If
            If mod1.isOverride Then
                tw.Fmt(EToken.Override)
            End If

            mod1.TokenListMod = tw.GetTokenList()
        End If
    End Sub
End Class
