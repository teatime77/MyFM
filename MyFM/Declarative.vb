﻿Imports System.Diagnostics

' -------------------------------------------------------------------------------- TNavi
Public Class TDeclarative
    Public RefCnt As Integer
    Public ErrNav As Boolean = False

    Public Sub IncRefCnt(ref1 As TReference)
        RefCnt += 1
    End Sub

    Public Overridable Sub StartCondition(self As Object)
    End Sub

    Public Overridable Sub EndCondition(self As Object)
    End Sub

    Public Overridable Sub NaviModifier(mod1 As TModifier)
        EndCondition(mod1)
    End Sub

    Public Overridable Sub NaviConstant(cns1 As TConstant)
        EndCondition(cns1)
    End Sub

    Public Overridable Sub NaviDot(dot1 As TDot)
        NaviTerm(dot1.TrmDot)

        EndCondition(dot1)
    End Sub

    Public Overridable Sub NaviReference(ref1 As TReference)
        EndCondition(ref1)
    End Sub

    Public Overridable Sub NaviTerm(trm1 As TTerm)
        If trm1 IsNot Nothing Then
            If TypeOf trm1 Is TStatement Then
                NaviStatement(CType(trm1, TStatement))
            Else
                StartCondition(trm1)

                Try
                    If TypeOf trm1 Is TConstant Then
                        NaviConstant(CType(trm1, TConstant))
                    ElseIf TypeOf trm1 Is TArray Then
                        NaviArray(CType(trm1, TArray))
                    ElseIf TypeOf trm1 Is TDot Then
                        NaviDot(CType(trm1, TDot))
                    ElseIf TypeOf trm1 Is TReference Then
                        NaviReference(CType(trm1, TReference))
                    ElseIf trm1.IsApp() Then
                        NaviApply(CType(trm1, TApply))
                    ElseIf trm1.IsLog() Then
                        NaviLog(CType(trm1, TApply))
                    ElseIf TypeOf trm1 Is TParenthesis Then
                        NaviTerm(CType(trm1, TParenthesis).TrmPar)
                        EndCondition(trm1)
                    ElseIf TypeOf trm1 Is TFrom Then
                        NaviFrom(CType(trm1, TFrom))
                        EndCondition(trm1)
                    ElseIf TypeOf trm1 Is TAggregate Then
                        NaviAggregate(CType(trm1, TAggregate))
                        EndCondition(trm1)
                    Else
                        Debug.Assert(False)
                    End If
                Catch ex As TError
                End Try
            End If

        End If
    End Sub

    Public Overridable Sub NaviTermList(list1 As TList(Of TTerm))
        StartCondition(list1)

        For Each trm1 In list1
            NaviTerm(trm1)
        Next
    End Sub

    Public Overridable Sub NaviCaseList(list1 As TList(Of TCase))
        StartCondition(list1)

        For Each cas1 In list1
            NaviStatement(cas1)
        Next
    End Sub

    Public Overridable Sub NaviStatementList(list1 As TList(Of TStatement))
        StartCondition(list1)

        For Each stmt1 In list1
            NaviStatement(stmt1)
        Next
    End Sub

    Public Overridable Sub NaviBlockList(list1 As TList(Of TBlock))
        StartCondition(list1)

        For Each blc_f In list1
            NaviStatement(blc_f)
        Next
    End Sub

    Public Overridable Sub NaviIfBlockList(list1 As TList(Of TIfBlock))
        StartCondition(list1)

        For Each blc_f In list1
            NaviStatement(blc_f)
        Next
    End Sub

    Public Overridable Sub NaviLocalVariableList(list1 As TList(Of TVariable))
        StartCondition(list1)

        For Each var1 In list1
            NaviLocalVariable(var1)
        Next
    End Sub

    Public Overridable Sub NaviLocalVariable(var1 As TVariable)
        If var1 IsNot Nothing Then

            StartCondition(var1)

            NaviTerm(var1.InitVar)

            EndCondition(var1)
        End If
    End Sub

    Public Overridable Sub NaviArray(arr1 As TArray)
        If arr1 IsNot Nothing Then
            NaviTermList(arr1.TrmArr)
            EndCondition(arr1)
        End If
    End Sub

    Public Overridable Sub NaviApply(app1 As TApply)
        If app1 IsNot Nothing Then
            NaviTermList(app1.ArgApp)

            If app1.FncApp IsNot Nothing Then
                NaviTerm(app1.FncApp)
            End If

            If app1.IniApp IsNot Nothing Then
                NaviTerm(app1.IniApp)
            End If

            EndCondition(app1)
        End If
    End Sub

    Public Overridable Sub NaviLog(opr1 As TApply)
        If opr1 IsNot Nothing Then
            NaviTermList(opr1.ArgApp)
            EndCondition(opr1)
        End If
    End Sub

    Public Overridable Sub NaviFrom(frm1 As TFrom)
        NaviTerm(frm1.SeqQry)
        NaviLocalVariable(frm1.VarQry)
        NaviTerm(frm1.CndQry)
        NaviTerm(frm1.SelFrom)
        NaviTerm(frm1.TakeFrom)
        NaviTerm(frm1.InnerFrom)
    End Sub

    Public Overridable Sub NaviAggregate(aggr1 As TAggregate)
        NaviTerm(aggr1.SeqQry)
        NaviLocalVariable(aggr1.VarQry)
        NaviTerm(aggr1.CndQry)
        NaviTerm(aggr1.IntoAggr)
    End Sub

    Public Overridable Sub NaviFor(for1 As TFor)
        NaviTerm(for1.IdxFor)
        NaviTerm(for1.InTrmFor)
        NaviLocalVariable(for1.InVarFor)
        NaviTerm(for1.FromFor)
        NaviTerm(for1.ToFor)
        NaviTerm(for1.StepFor)
        NaviStatement(for1.IniFor)
        NaviTerm(for1.CndFor)
        NaviStatement(for1.StepStmtFor)
        NaviStatement(for1.BlcFor)
    End Sub

    Public Overridable Sub NaviCase(cas1 As TCase)
        NaviTermList(cas1.TrmCase)
        NaviStatement(cas1.BlcCase)
    End Sub

    Public Overridable Sub NaviSelect(swt1 As TSelect)
        NaviTerm(swt1.TrmSel)
        NaviCaseList(swt1.CaseSel)
    End Sub

    Public Overridable Sub NaviAssignment(asn1 As TAssignment)
        NaviTerm(asn1.RelAsn)
    End Sub

    Public Overridable Sub NaviCall(call1 As TCall)
        NaviTerm(call1.AppCall)
    End Sub

    Public Overridable Sub NaviReDim(red1 As TReDim)
        NaviTerm(red1.TrmReDim)
        NaviTermList(red1.DimReDim)
    End Sub

    Public Overridable Sub NaviIf(if1 As TIf)
        NaviIfBlockList(if1.IfBlc)
    End Sub

    Public Overridable Sub NaviIfBlock(if_blc As TIfBlock)
        NaviTerm(if_blc.CndIf)
        NaviTerm(if_blc.WithIf)
        NaviStatement(if_blc.BlcIf)
    End Sub

    Public Overridable Sub NaviTry(try1 As TTry)
        NaviStatement(try1.BlcTry)
        NaviLocalVariableList(try1.VarCatch)
        NaviStatement(try1.BlcCatch)
    End Sub

    Public Overridable Sub NaviVariableDeclaration(dcl1 As TVariableDeclaration)
        NaviModifier(dcl1.ModDecl)
        NaviLocalVariableList(dcl1.VarDecl)
    End Sub

    Public Overridable Sub NaviReturn(ret1 As TReturn)
        NaviTerm(ret1.TrmRet)
    End Sub

    Public Overridable Sub NaviThrow(throw1 As TThrow)
        NaviTerm(throw1.TrmThrow)
    End Sub

    Public Overridable Sub NaviBlock(blc1 As TBlock)
        NaviStatementList(blc1.StmtBlc)
    End Sub

    Public Overridable Sub NaviStatement(stmt1 As TStatement)

        StartCondition(stmt1)

        If stmt1 IsNot Nothing Then

            If TypeOf stmt1 Is TAssignment Then
                NaviAssignment(CType(stmt1, TAssignment))
            ElseIf TypeOf stmt1 Is TCall Then
                NaviCall(CType(stmt1, TCall))
            ElseIf TypeOf stmt1 Is TVariableDeclaration Then
                NaviVariableDeclaration(CType(stmt1, TVariableDeclaration))
            ElseIf TypeOf stmt1 Is TReDim Then
                NaviReDim(CType(stmt1, TReDim))
            ElseIf TypeOf stmt1 Is TIf Then
                NaviIf(CType(stmt1, TIf))
            ElseIf TypeOf stmt1 Is TIfBlock Then
                NaviIfBlock(CType(stmt1, TIfBlock))
            ElseIf TypeOf stmt1 Is TSelect Then
                NaviSelect(CType(stmt1, TSelect))
            ElseIf TypeOf stmt1 Is TCase Then
                NaviCase(CType(stmt1, TCase))
            ElseIf TypeOf stmt1 Is TTry Then
                NaviTry(CType(stmt1, TTry))
            ElseIf TypeOf stmt1 Is TFor Then
                NaviFor(CType(stmt1, TFor))
            ElseIf TypeOf stmt1 Is TBlock Then
                NaviBlock(CType(stmt1, TBlock))
            ElseIf TypeOf stmt1 Is TReturn Then
                NaviReturn(CType(stmt1, TReturn))
            ElseIf TypeOf stmt1 Is TThrow Then
                NaviThrow(CType(stmt1, TThrow))
            ElseIf TypeOf stmt1 Is TComment Then
            Else
                Select Case stmt1.TypeStmt
                    Case EToken.ExitDo

                    Case EToken.ExitFor

                    Case EToken.ExitSub

                    Case Else
                        Debug.Assert(False)
                End Select
            End If
        End If

        EndCondition(stmt1)
    End Sub

    Public Overridable Sub NaviFunction(fnc1 As TFunction)
        StartCondition(fnc1)

        NaviStatement(fnc1.ComVar)
        NaviModifier(fnc1.ModFnc)
        NaviLocalVariableList(fnc1.ArgFnc)

        If fnc1.BlcFnc IsNot Nothing Then
            NaviStatement(fnc1.BlcFnc)
        End If

        EndCondition(fnc1)
    End Sub

    Public Overridable Sub NaviField(fld1 As TField)
        NaviStatement(fld1.ComVar)
        NaviModifier(fld1.ModVar)
        'NaviTerm(fld1.InitVar)

        EndCondition(fld1)
    End Sub

    Public Overridable Sub NaviClass(cla1 As TClass)
        If (cla1.FldCla.Count <> 0 OrElse cla1.FncCla.Count <> 0) AndAlso Not (cla1.GenCla IsNot Nothing AndAlso cla1.OrgCla Is Nothing) Then

            For Each fld1 In cla1.FldCla
                NaviField(fld1)
            Next

            '  すべてのメソッドに対し
            For Each fnc1 In cla1.FncCla
                NaviFunction(fnc1)
            Next

            EndCondition(cla1)
        End If
    End Sub

    Public Overridable Sub NaviSourceFile(src As TSourceFile)
        For Each cla1 In src.ClaSrc
            NaviClass(cla1)
        Next

        EndCondition(src)
    End Sub

    Public Overridable Sub NaviProject(prj As TProject)
        For Each src In prj.SrcPrj
            NaviSourceFile(src)
        Next
        '  すべてのクラスに対し
        'For Each cla1 In prj.SimpleParameterizedClassList
        '    NaviClass(cla1)
        'Next

        EndCondition(prj)
    End Sub

    Public Sub NaviAny(obj As Object)
        If TypeOf obj Is TProject Then
            NaviProject(CType(obj, TProject))
        ElseIf TypeOf obj Is TSourceFile Then
            NaviSourceFile(CType(obj, TSourceFile))
        ElseIf TypeOf obj Is TClass Then
            NaviClass(CType(obj, TClass))
        ElseIf TypeOf obj Is TField Then
            NaviField(CType(obj, TField))
        ElseIf TypeOf obj Is TFunction Then
            NaviFunction(CType(obj, TFunction))
        ElseIf TypeOf obj Is TLocalVariable Then
            NaviLocalVariable(CType(obj, TLocalVariable))
        ElseIf TypeOf obj Is TStatement Then
            NaviStatement(CType(obj, TStatement))
        ElseIf TypeOf obj Is TTerm Then
            NaviTerm(CType(obj, TTerm))
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TSetRefDeclarative
Public Class TSetRefDeclarative
    Inherits TDeclarative

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TFunction Then
            With CType(self, TFunction)
                If .InterfaceFnc IsNot Nothing Then
                    .ImplFnc.VarRef = TProject.FindFieldFunction(.InterfaceFnc, .ImplFnc.NameRef, Nothing)
                    Debug.Assert(.ImplFnc IsNot Nothing)
                End If

                Debug.Assert(.ThisFnc IsNot Nothing)
            End With

        End If
    End Sub

    Function InstanceOfIfBlock(var1 As TVariable, o As Object) As Boolean
        If TypeOf (o) Is TIfBlock Then
            Dim cnd As TTerm = CType(o, TIfBlock).CndIf

            If TypeOf (cnd) Is TApply Then
                Dim app1 As TApply = CType(cnd, TApply)

                If app1.TypeApp = EToken.Instanceof AndAlso TypeOf (app1.ArgApp(0)) Is TReference AndAlso CType(app1.ArgApp(0), TReference).VarRef Is var1 Then
                    Dim o2 As Object = app1.ArgApp(1)
                    Return True
                End If
            End If
        End If

        Return False
    End Function

    Public Sub SetTypeTrm(trm1 As TTerm)
        If trm1.CastType IsNot Nothing Then
            trm1.TypeTrm = trm1.CastType
        Else
            If TypeOf trm1 Is TConstant Then
                With CType(trm1, TConstant)
                    Select Case .TypeAtm
                        Case EToken.String_
                            .TypeTrm = .ProjectTrm.StringType
                        Case EToken.Int, EToken.Hex
                            .TypeTrm = .ProjectTrm.IntType
                        Case EToken.Char_
                            .TypeTrm = .ProjectTrm.CharType
                        Case Else
                            Debug.WriteLine("@h")
                            .TypeTrm = Nothing
                    End Select
                End With

            ElseIf TypeOf trm1 Is TArray Then

            ElseIf TypeOf trm1 Is TReference Then
                With CType(trm1, TReference)

                    If .VarRef IsNot Nothing Then
                        If TypeOf .VarRef Is TFunction Then
                            If .IsAddressOf Then
                                .TypeTrm = New TDelegate(.ProjectTrm, CType(.VarRef, TFunction))
                            Else
                                .TypeTrm = CType(.VarRef, TFunction).RetType
                                If .TypeTrm Is Nothing Then
                                    'Debug.Print("void型 {0}", .VarRef.NameVar)
                                End If
                            End If
                        ElseIf TypeOf .VarRef Is TClass Then
                            .TypeTrm = CType(.VarRef, TClass)
                        Else

                            .TypeTrm = .VarRef.TypeVar
                            Debug.Assert(.TypeTrm IsNot Nothing)

                            Dim v = From o In Sys.AncestorList(trm1) Where InstanceOfIfBlock(.VarRef, o)
                            If v.Any() Then
                                Dim if_blc As TIfBlock = v.First()
                                Dim tp1 As TClass = CType(CType(CType(if_blc.CndIf, TApply).ArgApp(1), TReference).VarRef, TClass)
                                If tp1 IsNot Nothing AndAlso tp1.IsSubcla(.VarRef.TypeVar) Then
                                    .TypeTrm = tp1
                                End If
                            End If
                        End If
                    End If
                End With

            ElseIf TypeOf trm1 Is TApply Then
                With CType(trm1, TApply)
                    Select Case .TypeApp
                        Case EToken.And_, EToken.OR_, EToken.Not_, EToken.Anp
                            .TypeTrm = .ProjectTrm.BoolType

                        Case EToken.Eq, EToken.NE, EToken.ASN, EToken.LT, EToken.GT, EToken.ADDEQ, EToken.SUBEQ, EToken.MULEQ, EToken.DIVEQ, EToken.MODEQ, EToken.LE, EToken.GE, EToken.IsNot_, EToken.Instanceof, EToken.Is_
                            .TypeTrm = .ProjectTrm.BoolType

                        Case EToken.ADD, EToken.Mns, EToken.MUL, EToken.DIV, EToken.MOD_, EToken.INC, EToken.DEC, EToken.BitOR
                            .TypeTrm = .ArgApp(0).TypeTrm

                        Case EToken.AppCall
                            If TypeOf .FncApp Is TReference Then

                                Dim ref1 As TReference = CType(.FncApp, TReference)
                                If TypeOf ref1.VarRef Is TFunction Then
                                    Dim fnc1 As TFunction = CType(ref1.VarRef, TFunction)
                                    If fnc1.RetType IsNot Nothing AndAlso fnc1.RetType.ContainsArgumentClass Then

                                        Dim dic As New Dictionary(Of String, TClass)
                                        Dim vidx = From idx In Sys.IndexList(fnc1.ArgFnc) Where fnc1.ArgFnc(idx).TypeVar.ContainsArgumentClass
                                        Debug.Assert(vidx.Any())
                                        Dim i1 As Integer = vidx.First()
                                        Dim tp1 As TClass = fnc1.ArgFnc(i1).TypeVar
                                        Dim tp2 As TClass = .ArgApp(i1).TypeTrm
                                        Debug.Assert(tp1.OrgCla Is tp2.OrgCla)
                                        Dim i2 As Integer
                                        For i2 = 0 To tp1.GenCla.Count - 1
                                            dic.Add(tp1.GenCla(i2).NameVar, tp2.GenCla(i2))
                                        Next

                                        .TypeTrm = .ProjectTrm.SubstituteArgumentClass(fnc1.RetType, dic)
                                    Else
                                        .TypeTrm = fnc1.RetType
                                    End If
                                Else
                                    Select Case .KndApp
                                        Case EApply.CallApp
                                            Debug.Assert(TypeOf .FncApp.TypeTrm Is TDelegate)
                                            .TypeTrm = CType(.FncApp.TypeTrm, TDelegate).RetDlg
                                        Case EApply.ArrayApp
                                            Debug.Assert(ref1.VarRef.TypeVar.GenCla IsNot Nothing AndAlso ref1.VarRef.TypeVar.GenCla.Count = 1)
                                            .TypeTrm = ref1.VarRef.TypeVar.GenCla(0)
                                        Case EApply.StringApp
                                            .TypeTrm = .ProjectTrm.CharType
                                        Case EApply.ListApp
                                            Debug.Assert(ref1.VarRef.TypeVar.GenCla IsNot Nothing AndAlso ref1.VarRef.TypeVar.GenCla.Count = 1)
                                            .TypeTrm = ref1.VarRef.TypeVar.GenCla(0)
                                        Case EApply.DictionaryApp
                                            Debug.Assert(ref1.VarRef.TypeVar.GenCla IsNot Nothing AndAlso ref1.VarRef.TypeVar.GenCla.Count = 2)
                                            If ref1.VarRef.TypeVar.NameVar = "TMap" Then

                                                .TypeTrm = .ProjectTrm.ElementType(ref1.VarRef.TypeVar)
                                            Else
                                                .TypeTrm = ref1.VarRef.TypeVar.GenCla(1)
                                            End If
                                        Case Else
                                            Debug.Assert(False)
                                    End Select
                                End If
                            Else
                                Dim cla1 As TClass

                                cla1 = .FncApp.TypeTrm
                                If cla1 Is .ProjectTrm.StringType Then
                                    .TypeTrm = .ProjectTrm.CharType
                                Else
                                    .TypeTrm = cla1
                                End If
                            End If

                        Case EToken.BaseCall
                            .TypeTrm = Nothing

                        Case EToken.BaseNew
                            .TypeTrm = Nothing

                        Case EToken.As_
                            .TypeTrm = .ClassApp

                        Case EToken.Question
                            .TypeTrm = .ArgApp(1).TypeTrm

                        Case EToken.Instanceof
                            .TypeTrm = .ProjectTrm.BoolType

                        Case EToken.New_
                            .TypeTrm = .NewApp

                        Case EToken.GetType_
                            .TypeTrm = .ProjectTrm.TypeType

                        Case Else
                            Debug.WriteLine("Err Trm Src2:{0}", .TypeApp)
                            Debug.Assert(False)
                    End Select
                End With

            ElseIf TypeOf trm1 Is TParenthesis Then
                With CType(trm1, TParenthesis)
                    .TypeTrm = .TrmPar.TypeTrm
                End With

            ElseIf TypeOf trm1 Is TFrom Then
                With CType(trm1, TFrom)
                    Dim cla1 As TClass, cla2 As TClass

                    If .InnerFrom IsNot Nothing Then
                        .TypeTrm = .InnerFrom.TypeTrm
                    Else
                        If .SelFrom Is Nothing Then
                            cla1 = .SeqQry.TypeTrm
                            Debug.Assert(cla1 IsNot Nothing)
                            cla2 = .ProjectTrm.ElementType(cla1)
                            .TypeTrm = .ProjectTrm.GetIEnumerableClass(cla2)
                        Else
                            cla1 = .SelFrom.TypeTrm
                            Debug.Assert(cla1 IsNot Nothing)
                            .TypeTrm = .ProjectTrm.GetIEnumerableClass(cla1)
                        End If
                    End If
                End With

            ElseIf TypeOf trm1 Is TAggregate Then
                With CType(trm1, TAggregate)

                    .TypeTrm = .IntoAggr.TypeTrm
                End With

            Else
                Debug.Assert(False)
            End If
        End If
    End Sub

    Public Function GetWithClass(self As TDot) As TClass
        Dim if_blc = From o In Sys.AncestorList(self) Where TypeOf o Is TIfBlock AndAlso CType(o, TIfBlock).WithIf IsNot Nothing

        If if_blc.Any() Then
            Return CType(if_blc.First(), TIfBlock).WithIf.TypeTrm
        End If

        Return self.FunctionTrm.WithFnc
    End Function

    Public Overrides Sub EndCondition(self As Object)
        If TypeOf self Is TStatement Then

        ElseIf TypeOf self Is TTerm Then
            Dim trm1 As TTerm = CType(self, TTerm)

            If TypeOf self Is TConstant Then
                SetTypeTrm(trm1)

            ElseIf TypeOf self Is TDot Then
                With CType(self, TDot)
                    IncRefCnt(self)

                    If .TrmDot Is Nothing Then

                        .TypeDot = GetWithClass(CType(self, TDot))
                        Debug.Assert(.TypeDot IsNot Nothing)
                    Else

                        .TypeDot = .TrmDot.TypeTrm
                    End If

                    If .TypeDot Is Nothing Then
                        Throw New TError(String.Format("ドットの左の項の型が不明 {0}", .NameRef))
                    End If
                    If TypeOf .TypeDot Is TDelegate Then
                        Debug.Assert(.NameRef = "Invoke")
                    End If

                    Dim obj As Object = Sys.UpObj(self)
                    Dim dot_is_fncapp As Boolean = (TypeOf obj Is TApply AndAlso self Is CType(obj, TApply).FncApp)
                    If dot_is_fncapp Then
                        Dim app1 As TApply = CType(obj, TApply)

                        Debug.Assert(self Is app1.FncApp)
                        Debug.Assert(app1.TypeApp = EToken.AppCall)

                        If .IsAddressOf Then
                            .VarRef = TProject.FindFieldFunction(.TypeDot, .NameRef, Nothing)
                        Else
                            .VarRef = TProject.FindFieldFunction(.TypeDot, .NameRef, app1.ArgApp)
                        End If

                        If .VarRef Is Nothing Then
                            Throw New TError(String.Format("不明なメンバー {0} {1}", .TypeDot.LongName(), .NameRef))
                        Else
                            Debug.Assert(TypeOf .VarRef Is TFunction OrElse TypeOf .VarRef.TypeVar Is TDelegate OrElse .VarRef.TypeVar.HasIndex())
                        End If
                    Else

                        .VarRef = TProject.FindFieldFunction(.TypeDot, .NameRef, Nothing)
                    End If


                    If .VarRef Is Nothing Then
                        Throw New TError(String.Format("不明なメンバー {0} {1}", .TypeDot.LongName(), .NameRef))
                    Else
                        If Not dot_is_fncapp AndAlso TypeOf .VarRef Is TFunction Then
                            Debug.WriteLine("メソッドを値として参照 {0}", .VarRef.NameVar)
                        End If
                    End If

                    SetTypeTrm(trm1)
                End With

            ElseIf TypeOf self Is TReference Then
                With CType(self, TReference)
                    Dim ref1 As TReference = CType(self, TReference)
                    IncRefCnt(ref1)
                    Dim obj As Object = Sys.UpObj(ref1)
                    If TypeOf obj Is TApply Then
                        Dim app1 As TApply = CType(obj, TApply)

                        If ref1 Is app1.FncApp Then
                            Select Case app1.TypeApp
                                Case EToken.ADD, EToken.Mns, EToken.MUL, EToken.DIV, EToken.MOD_, EToken.INC, EToken.DEC, EToken.BitOR

                                    If .VarRef Is Nothing Then

                                        ' 演算子オーバーロード関数を得る
                                        Dim fnc1 As TFunction = app1.ProjectTrm.GetOperatorFunction(app1.TypeApp, app1.ArgApp(0))
                                        If fnc1 IsNot Nothing Then
                                            ' 演算子オーバーロード関数を得られた場合

                                            .VarRef = fnc1
                                        Else
                                            ' 演算子オーバーロード関数を得られない場合

                                            Dim name1 As String = ""
                                            Select Case app1.TypeApp
                                                Case EToken.ADD
                                                    name1 = "__Add"
                                                Case EToken.Mns
                                                    name1 = "__Mns"
                                                Case EToken.MUL
                                                    name1 = "__Mul"
                                                Case EToken.DIV
                                                    name1 = "__Div"
                                                Case EToken.MOD_
                                                    name1 = "__Mod"
                                                Case EToken.INC
                                                    name1 = "__Inc"
                                                Case EToken.DEC
                                                    name1 = "__Dec"
                                                Case EToken.BitOR
                                                    name1 = "__BitOr"
                                            End Select

                                            Dim vvar = (From fnc In app1.ProjectTrm.SystemType.FncCla Where fnc.NameVar = name1).ToList()
                                            If vvar.Count <> 1 Then

                                                Debug.WriteLine("演算子未定義:{0}", .NameRef)
                                                Debug.Assert(False)
                                            Else
                                                .VarRef = vvar(0)
                                            End If
                                        End If

                                        If .VarRef Is Nothing Then
                                            Debug.WriteLine("演算子未定義:{0}", .NameRef)
                                        End If
                                    End If

                                Case EToken.AppCall
                                    .VarRef = TProject.FindFieldFunction(app1.FunctionTrm.ClaFnc, .NameRef, app1.ArgApp)
                                    If .VarRef Is Nothing Then

                                        .VarRef = .ProjectTrm.FindVariable(ref1, .NameRef)
                                        If .VarRef Is Nothing Then

                                            Debug.WriteLine("関数呼び出し 未定義:{0}", .NameRef)
                                        End If
                                    End If

                                Case EToken.New_
                                    If app1.NewApp.DimCla = 0 Then
                                        .VarRef = TProject.FindNew(app1.NewApp, app1.ArgApp)
                                        'AndAlso app1.ArgApp.Count <> 0 AndAlso app1.NewApp.DimCla = 0
                                        If .VarRef Is Nothing Then
                                            Debug.WriteLine("New 未定義 {0} {1}", .NameRef, app1.ArgApp.Count)
                                        End If
                                    Else
                                        .VarRef = .ProjectTrm.ArrayMaker
                                    End If
                                    Return

                                Case EToken.BaseCall
                                    .VarRef = TProject.FindFieldFunction(.FunctionTrm.ClaFnc.DirectSuperClassList(0), .NameRef, app1.ArgApp)
                                    Debug.Assert(.VarRef IsNot Nothing AndAlso TypeOf .VarRef Is TFunction)
                                    Return

                                Case EToken.BaseNew
                                    .VarRef = TProject.FindNew(.FunctionTrm.ClaFnc.DirectSuperClassList(0), app1.ArgApp)
                                    If .VarRef Is Nothing Then
                                        If app1.ArgApp.Count <> 0 Then

                                            Debug.WriteLine("New 未定義 {0} {1}", .NameRef, app1.ArgApp.Count)
                                        End If
                                    End If

                            End Select
                        End If
                    End If

                    If .VarRef Is Nothing Then
                        .VarRef = TProject.FindFieldFunction(.FunctionTrm.ClaFnc, .NameRef, Nothing)
                        If .VarRef Is Nothing Then

                            .VarRef = .ProjectTrm.FindVariable(ref1, .NameRef)
                            If .VarRef Is Nothing Then
                                '.VarRef = .ProjectTrm.FindVariable(ref1, .NameRef)
                                '.VarRef = TProject.FindFieldFunction(.FunctionTrm.ClaFnc, .NameRef, Nothing)
                                Debug.WriteLine("変数未定義:{0}", .NameRef)
                            End If
                        End If
                    End If

                    SetTypeTrm(trm1)
                End With

            ElseIf TypeOf self Is TApply Then
                With CType(self, TApply)
                    Select Case .TypeApp
                        Case EToken.AppCall
                            If .FncApp IsNot Nothing Then

                                If TypeOf .FncApp Is TReference AndAlso TypeOf CType(.FncApp, TReference).VarRef Is TFunction Then
                                    ' 関数呼び出しの場合

                                    .KndApp = EApply.CallApp

                                ElseIf TypeOf .FncApp Is TReference AndAlso TypeOf (CType(.FncApp, TReference).VarRef.TypeVar) Is TDelegate Then
                                    .KndApp = EApply.CallApp
                                Else
                                    ' 関数呼び出しでない場合

                                    Dim cla1 As TClass = .FncApp.TypeTrm
                                    Debug.Assert(cla1 IsNot Nothing)

                                    If cla1.NameCla() = "String" Then
                                        .KndApp = EApply.StringApp
                                    ElseIf cla1.IsArray() Then
                                        .KndApp = EApply.ArrayApp
                                    ElseIf cla1.IsList() Then
                                        .KndApp = EApply.ListApp
                                    ElseIf cla1.IsDictionary() Then
                                        .KndApp = EApply.DictionaryApp
                                    Else
                                        Debug.Print("想定外 1")
                                    End If
                                End If

                            Else
                                Debug.Print("想定外 2")
                            End If

                        Case EToken.ADD, EToken.Mns, EToken.MUL, EToken.DIV, EToken.MOD_, EToken.INC, EToken.DEC, EToken.BitOR, EToken.Question
                        Case EToken.New_, EToken.Cast, EToken.GetType_, EToken.BaseNew, EToken.BaseCall
                        Case Else
                            If .IsLog() Then
                            Else
                                Debug.Print("想定外 3")
                            End If

                    End Select

                    SetTypeTrm(trm1)
                End With

            ElseIf TypeOf trm1 Is TParenthesis Then
                SetTypeTrm(trm1)

            ElseIf TypeOf trm1 Is TFrom Then
                SetTypeTrm(trm1)

            ElseIf TypeOf trm1 Is TAggregate Then
                SetTypeTrm(trm1)

            End If

        ElseIf TypeOf self Is TClass Then

        ElseIf TypeOf self Is TFunction Then

        ElseIf TypeOf self Is TVariable Then
            With CType(self, TVariable)
                Dim obj As Object = Sys.UpObj(self)

                If TypeOf obj Is TFrom Then
                    Dim frm1 As TFrom = CType(obj, TFrom)
                    Debug.Assert(self Is frm1.VarQry)

                    Dim type1 As TClass = frm1.SeqQry.TypeTrm
                    .TypeVar = frm1.ProjectTrm.ElementType(type1)
                    Debug.Assert(.TypeVar IsNot Nothing)

                ElseIf TypeOf obj Is TAggregate Then
                    Dim aggr1 As TAggregate = CType(obj, TAggregate)
                    Debug.Assert(self Is aggr1.VarQry)

                    Dim type1 As TClass = aggr1.SeqQry.TypeTrm
                    .TypeVar = aggr1.ProjectTrm.ElementType(type1)
                    Debug.Assert(.TypeVar IsNot Nothing)

                ElseIf TypeOf obj Is TFor Then
                    Dim for1 As TFor = CType(obj, TFor)
                    Debug.Assert(self Is for1.InVarFor)

                    Dim type1 As TClass = for1.InTrmFor.TypeTrm
                    .TypeVar = for1.ProjectStmt().ElementType(type1)
                    Debug.Assert(.TypeVar IsNot Nothing)

                Else

                    Dim vstmt = From up_self In Sys.AncestorList(self) Where TypeOf up_self Is TVariableDeclaration

                    If vstmt.Any() Then
                        If .InitVar IsNot Nothing Then
                            If .TypeVar Is Nothing Then
                                .NoType = True
                                .TypeVar = .InitVar.TypeTrm
                            End If
                        End If
                    End If
                End If
            End With
        End If
    End Sub
End Class


' -------------------------------------------------------------------------------- TNaviSetDefRef
Public Class TNaviSetDefRef2
    Inherits TDeclarative

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TReference Then

            With CType(self, TReference)

                Dim up_stmt As TStatement = Sys.UpToStmt(self)
                If TypeOf up_stmt Is TAssignment Then
                    Dim asn1 As TAssignment = CType(up_stmt, TAssignment)

                    If TypeOf asn1.RelAsn.ArgApp(0) Is TReference Then
                        ' 左辺が変数参照の場合

                        If asn1.RelAsn.ArgApp(0) Is self Then
                            .DefRef = True
                        End If
                    Else
                        ' 左辺が関数呼び出しの場合

                        Debug.Assert(asn1.RelAsn.ArgApp(0).IsApp())
                        Dim app1 As TApply = CType(asn1.RelAsn.ArgApp(0), TApply)

                        Debug.Assert(app1.KndApp = EApply.ArrayApp OrElse app1.KndApp = EApply.ListApp)

                        If app1.FncApp Is self Then

                            .DefRef = True
                        End If
                    End If
                End If
            End With
        End If
    End Sub
End Class


' -------------------------------------------------------------------------------- TNaviSetVarRef
Public Class TNaviSetVarRefNEW
    Inherits TDeclarative

    Public Overrides Sub EndCondition(self As Object)
        If TypeOf self Is TDot Then
            With CType(self, TReference)
                Debug.Assert(.VarRef IsNot Nothing)
                Debug.Assert(Not .VarRef.RefVar.Contains(CType(self, TReference)))

                .VarRef.RefVar.Add(CType(self, TReference))
            End With

        ElseIf TypeOf self Is TReference Then
            With CType(self, TReference)
                Debug.Assert(.VarRef IsNot Nothing)
                Debug.Assert(Not .VarRef.RefVar.Contains(CType(self, TReference)))

                .VarRef.RefVar.Add(CType(self, TReference))
            End With
        End If
    End Sub
End Class


' -------------------------------------------------------------------------------- TNaviSetProjectTrm
Public Class TNaviSetProjectTrm
    Inherits TDeclarative

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TStatement Then

        ElseIf TypeOf self Is TTerm Then
            With CType(self, TTerm)
                Dim prj = From obj In Sys.AncestorList(self) Where TypeOf obj Is TProject
                .ProjectTrm = CType(prj.First(), TProject)
            End With
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetLabel
Public Class TNaviSetLabel
    Inherits TDeclarative

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TExit Then
            With CType(self, TExit)

                If .TypeStmt = EToken.ExitDo OrElse .TypeStmt = EToken.ExitFor Then
                    Dim for_do As TFor = Nothing

                    Dim for_select As Object = (From obj In Sys.AncestorList(self) Where TypeOf (obj) Is TFor OrElse TypeOf obj Is TSelect).First()
                    Select Case .TypeStmt
                        Case EToken.ExitDo
                            If Not (TypeOf for_select Is TFor AndAlso CType(for_select, TFor).IsDo) Then
                                ' 直近のSelectまたはループがDoでない場合

                                ' 直近のDoを探す。
                                for_do = CType((From obj In Sys.AncestorList(self) Where TypeOf (obj) Is TFor AndAlso CType(obj, TFor).IsDo).First(), TFor)
                            End If

                        Case EToken.ExitFor
                            If Not (TypeOf for_select Is TFor AndAlso Not CType(for_select, TFor).IsDo) Then
                                ' 直近のSelectまたはループがForでない場合

                                ' 直近のForを探す。
                                for_do = CType((From obj In Sys.AncestorList(self) Where TypeOf (obj) Is TFor AndAlso Not CType(obj, TFor).IsDo).First(), TFor)
                            End If
                    End Select

                    If for_do IsNot Nothing Then
                        ' 直近のSelectまたはループがこのExitに対応しない場合

                        If for_do.LabelFor = 0 Then
                            ' ForまたはDoにラベル番号をつけていない場合

                            ' 関数ごとのラベル番号をカウントアップする。
                            .FunctionTrm.LabelCount = .FunctionTrm.LabelCount + 1

                            ' ForまたはDoにラベル番号をつける。
                            for_do.LabelFor = .FunctionTrm.LabelCount
                        End If

                        ' Exit文にラベル番号を指定する。
                        .LabelExit = for_do.LabelFor
                    End If
                End If
            End With
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetRefStmt
' RefStmtをセットする。
Public Class TNaviSetRefStmt
    Inherits TDeclarative
    Public CurrentStatement As TStatement

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TDot Then
            With CType(self, TDot)
                If CurrentStatement IsNot Nothing Then
                    If .TrmDot Is Nothing OrElse Not TypeOf .TrmDot Is TDot Then
                        If .TrmDot Is Nothing Then
                        ElseIf TypeOf .TrmDot Is TReference Then
                        ElseIf TypeOf .TrmDot Is TApply OrElse TypeOf .TrmDot Is TParenthesis Then
                        Else
                            Debug.Assert(False)
                        End If

                        Dim outer_most_dot As TDot = Sys.OuterMostDot(CType(self, TDot))
                        .DefRef = outer_most_dot.DefRef
                        Debug.Assert(Not CurrentStatement.RefStmt.Contains(CType(self, TDot)))
                        CurrentStatement.RefStmt.Add(CType(self, TDot))
                    End If
                Else
                    Debug.Assert(False)
                End If
            End With

        ElseIf TypeOf self Is TReference Then
            With CType(self, TReference)
                If CurrentStatement IsNot Nothing Then
                    Debug.Assert(Not CurrentStatement.RefStmt.Contains(CType(self, TReference)))
                    CurrentStatement.RefStmt.Add(CType(self, TReference))
                Else
                    Debug.Assert(False)
                End If
            End With

        ElseIf TypeOf self Is TStatement Then
            If TypeOf self Is TAssignment OrElse TypeOf self Is TIfBlock OrElse TypeOf self Is TCall OrElse TypeOf self Is TVariableDeclaration OrElse TypeOf self Is TSelect OrElse TypeOf self Is TCase Then
            ElseIf TypeOf self Is TComment OrElse TypeOf self Is TBlock OrElse TypeOf self Is TIf Then
            ElseIf TypeOf self Is TReturn OrElse TypeOf self Is TFor OrElse TypeOf self Is TReDim OrElse TypeOf self Is TExit OrElse TypeOf self Is TThrow OrElse TypeOf self Is TTry Then
            Else
                Debug.Assert(False)
            End If

            CurrentStatement = CType(self, TStatement)
            CurrentStatement.RefStmt.Clear()
        End If
    End Sub

    Public Overrides Sub EndCondition(self As Object)
        If TypeOf self Is TStatement Then
            CurrentStatement = Nothing
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviAllRefStmt
' すべてのRefStmtを得る。
Public Class TNaviAllRefStmt
    Inherits TDeclarative
    Public RefStmtList As New TList(Of TReference)

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TStatement Then
            With CType(self, TStatement)
                RefStmtList.AddRange(.RefStmt)
            End With
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviTestReplaceTerm
' ReplaceTermのテスト
Public Class TNaviTestReplaceTerm
    Inherits TDeclarative
    Public RefStmtList As New TList(Of TReference)

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TTerm Then
            Sys.ReplaceTerm(CType(self, TTerm), CType(self, TTerm))
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetVirtualizableIf
' クラスの場合分けのIf文を探す。
Public Class TNaviSetVirtualizableIf
    Inherits TDeclarative
    Public VirtualizableClassList As New TList(Of TClass)

    Public Function IsVirtualizableIfBlock(if_blc As TIfBlock) As Boolean
        If TypeOf if_blc.CndIf Is TApply Then
            Dim app1 As TApply = CType(if_blc.CndIf, TApply)

            If TypeOf app1.ArgApp(0) Is TReference AndAlso app1.TypeApp = EToken.Instanceof Then
                Dim ref1 As TReference = CType(app1.ArgApp(0), TReference)

                If ref1.VarRef Is if_blc.FunctionTrm.ArgFnc(0) Then
                    Return True
                End If
            End If
        End If
        Return False
    End Function

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TIf Then
            With CType(self, TIf)
                Dim may_be_virtualizable_if As Boolean = False

                Dim up_stmt As TStatement = Sys.UpStmtProper(.UpTrm)

                If up_stmt Is Nothing Then
                    may_be_virtualizable_if = True
                Else
                    If TypeOf up_stmt Is TIfBlock Then
                        may_be_virtualizable_if = CType(CType(up_stmt, TIfBlock).UpTrm, TIf).VirtualizableIf
                    Else
                        may_be_virtualizable_if = False
                    End If
                End If

                If may_be_virtualizable_if Then
                    Dim virtualizable_if_block_list = From x In .IfBlc Where IsVirtualizableIfBlock(x)

                    If virtualizable_if_block_list.Count() = .IfBlc.Count Then
                        Dim virtualizable_class_list = From x In .IfBlc Select CType(CType(CType(x.CndIf, TApply).ArgApp(1), TReference).VarRef, TClass)
                        VirtualizableClassList.DistinctAddRange(virtualizable_class_list)

                        .VirtualizableIf = True
                    Else
                        .VirtualizableIf = False
                    End If
                Else
                    .VirtualizableIf = False
                End If
            End With
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetDependency
' 依存関係をセットする。
Public Class TNaviSetDependency
    Inherits TDeclarative

    Public Overrides Sub EndCondition(self As Object)
        If TypeOf self Is TStatement Then

        ElseIf TypeOf self Is TTerm Then
            With CType(self, TTerm)
                .DependTrm = New TDependency()

                If TypeOf self Is TDot Then
                    With CType(self, TDot)
                        Debug.Assert(.VarRef IsNot Nothing)

                        If .TrmDot Is Nothing Then
                            .DependTrm.SourceList.Add(New TDependency(EDependency.Self))
                        Else
                        End If
                    End With

                ElseIf TypeOf self Is TReference Then
                    With CType(self, TReference)
                        Debug.Assert(.VarRef IsNot Nothing)

                        If TypeOf .VarRef Is TLocalVariable Then

                        ElseIf TypeOf .VarRef Is TClass Then

                        ElseIf TypeOf .VarRef Is TFunction Then

                        ElseIf TypeOf .VarRef Is TField Then

                        Else
                            Debug.Assert(False)
                        End If
                    End With

                ElseIf TypeOf self Is TConstant Then

                ElseIf TypeOf self Is TFrom Then
                    With CType(self, TFrom)

                    End With

                ElseIf TypeOf self Is TAggregate Then
                    With CType(self, TAggregate)

                    End With

                ElseIf TypeOf self Is TApply Then

                ElseIf TypeOf self Is TParenthesis Then

                Else
                    Debug.Print("")
                End If

            End With

        ElseIf TypeOf self Is TVariable Then
            With CType(self, TVariable)
                .DependVar = New TDependency()

                If TypeOf self Is TLocalVariable Then
                    If TypeOf .UpVar Is TList(Of TVariable) Then
                        Dim up_obj As Object = Sys.UpObj(.UpVar)

                        If TypeOf up_obj Is TFunction Then
                            Dim fnc1 As TFunction = CType(up_obj, TFunction)

                            Dim k As Integer = fnc1.ArgFnc.IndexOf(CType(self, TVariable))
                            Select Case k
                                Case 0

                                Case 1

                                Case Else
                                    Debug.Assert(False)
                            End Select

                        ElseIf TypeOf up_obj Is TVariableDeclaration Then
                            If .InitVar IsNot Nothing Then
                            End If

                        ElseIf TypeOf up_obj Is TTry Then
                            Debug.Assert(False)

                        Else
                            Debug.Assert(False)
                        End If

                    ElseIf TypeOf .UpVar Is TFrom Then
                        Dim from1 As TFrom = CType(.UpVar, TFrom)

                    ElseIf TypeOf .UpVar Is TAggregate Then
                        Dim aggr1 As TAggregate = CType(.UpVar, TAggregate)

                    ElseIf TypeOf .UpVar Is TFor Then
                        Debug.Assert(False)

                    Else
                        Debug.Assert(False)
                    End If

                ElseIf TypeOf self Is TFunction Then
                    With CType(self, TFunction)
                        Debug.Assert(.ModVar.isInvariant)
                    End With
                Else
                    Debug.Assert(False)
                End If
            End With
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetRefPath
' 参照パスをセットする。
Public Class TNaviSetRefPath
    Inherits TDeclarative

    Public Overrides Sub EndCondition(self As Object)
        If TypeOf self Is TStatement Then

        ElseIf TypeOf self Is TTerm Then
            With CType(self, TTerm)
                .RefPathTrm = New TRefPath()

                If TypeOf self Is TDot Then
                    With CType(self, TDot)
                        Debug.Assert(.VarRef IsNot Nothing)

                        If .TrmDot Is Nothing Then
                            .RefPathTrm.RefPathType = ERefPathType.SelfField
                        ElseIf Not TypeOf .TrmDot Is TDot Then
                            Select Case .TrmDot.RefPathTrm.RefPathType
                                Case ERefPathType.Self
                                    .RefPathTrm.RefPathType = ERefPathType.SelfField

                                Case ERefPathType.SelfField, ERefPathType.SelfFieldSubField
                                    .RefPathTrm.RefPathType = ERefPathType.SelfFieldSubField

                                Case ERefPathType.Parent
                                    .RefPathTrm.RefPathType = ERefPathType.ParentField

                                Case ERefPathType.Prev
                                    .RefPathTrm.RefPathType = ERefPathType.PrevField

                                Case ERefPathType.App, ERefPathType.AppField
                                    .RefPathTrm.RefPathType = ERefPathType.AppField

                                Case ERefPathType.ClassRef
                                    .RefPathTrm.RefPathType = ERefPathType.GlobalRef

                                Case Else
                                    Debug.Print("{0}", .TrmDot.RefPathTrm.RefPathType)

                            End Select
                        End If
                    End With

                ElseIf TypeOf self Is TReference Then
                    With CType(self, TReference)
                        Debug.Assert(.VarRef IsNot Nothing)

                        If TypeOf .VarRef Is TLocalVariable Then

                            Debug.Assert(.VarRef.RefPathVar.RefPathType <> ERefPathType.Unknown)
                            .RefPathTrm = .VarRef.RefPathVar

                        ElseIf TypeOf .VarRef Is TClass Then
                            .RefPathTrm.RefPathType = ERefPathType.ClassRef

                        ElseIf TypeOf .VarRef Is TFunction Then
                            .RefPathTrm.RefPathType = ERefPathType.Apply

                        ElseIf TypeOf .VarRef Is TField Then
                            If CType(.VarRef, TField).ClaFld Is TProject.Prj.SystemType Then
                                .RefPathTrm.RefPathType = ERefPathType.GlobalRef
                            Else
                                Debug.Assert(False)
                            End If

                        Else
                            Debug.Assert(False)
                        End If
                    End With

                ElseIf TypeOf self Is TConstant Then
                    .RefPathTrm.RefPathType = ERefPathType.Constant

                ElseIf TypeOf self Is TFrom Then
                    With CType(self, TFrom)

                    End With

                ElseIf TypeOf self Is TAggregate Then
                    With CType(self, TAggregate)

                    End With

                ElseIf TypeOf self Is TApply Then
                    .RefPathTrm.RefPathType = ERefPathType.Apply

                Else
                    Debug.Print("")
                End If

                If .RefPathTrm.RefPathType = ERefPathType.Unknown Then
                    Debug.Print("")
                End If

            End With

        ElseIf TypeOf self Is TVariable Then
            With CType(self, TVariable)
                .RefPathVar = New TRefPath()

                If TypeOf self Is TField Then
                    With CType(self, TField)

                    End With
                Else
                    If TypeOf .UpVar Is TList(Of TVariable) Then
                        Dim up_obj As Object = Sys.UpObj(.UpVar)

                        If TypeOf up_obj Is TFunction Then
                            Dim fnc1 As TFunction = CType(up_obj, TFunction)

                            Dim k As Integer = fnc1.ArgFnc.IndexOf(CType(self, TVariable))
                            Select Case k
                                Case 0
                                    .RefPathVar.RefPathType = ERefPathType.Self

                                Case 1
                                    .RefPathVar.RefPathType = ERefPathType.App

                                Case Else
                                    Debug.Assert(False)
                            End Select

                        ElseIf TypeOf up_obj Is TVariableDeclaration Then
                            If .InitVar IsNot Nothing Then
                                .RefPathVar = .InitVar.RefPathTrm
                            End If

                        ElseIf TypeOf up_obj Is TTry Then
                            Debug.Assert(False)
                        Else
                            Debug.Assert(False)
                            Debug.Print("----------------------------------------- UpVar List {0}", up_obj.GetType())
                        End If

                    ElseIf TypeOf .UpVar Is TQuery Then
                        Dim qry1 As TQuery = CType(.UpVar, TQuery)

                        Select Case qry1.SeqQry.RefPathTrm.RefPathType
                            Case ERefPathType.SelfField
                                qry1.VarQry.RefPathVar.RefPathType = ERefPathType.SelfFieldChild

                            Case ERefPathType.SelfFieldChildField
                                qry1.VarQry.RefPathVar.RefPathType = ERefPathType.SelfFieldChildFieldChild

                            Case Else
                                Debug.Assert(False)
                        End Select

                    ElseIf TypeOf .UpVar Is TAggregate Then
                        Dim aggr1 As TAggregate = CType(.UpVar, TAggregate)

                    ElseIf TypeOf .UpVar Is TFor Then
                        Debug.Assert(False)

                    Else
                        Debug.Assert(False)
                        Debug.Print("----------------------------------------- UpVar {0}", .UpVar.GetType())
                    End If

                End If

            End With
        End If
    End Sub
End Class


' -------------------------------------------------------------------------------- TNaviMakeVirtualizableIfMethod
' クラスの場合分けのIf文からクラスごとのメソッドを作る。
Public Class TNaviMakeVirtualizableIfMethod
    Inherits TDeclarative
    Public OrgRuleNavi As TFunction
    Public UDA As TUseDefineAnalysis

    Public Function CopyAncestorBlock(if1 As TIf, blc1 As TBlock, cpy As TCopy) As TBlock
        Dim up_blc As TBlock = Sys.UpBlock(if1)
        Dim up_blc_copy As New TBlock

        ' up_blcの変数をup_blc_copyにコピーする。
        Dim vvar1 = From var1 In up_blc.VarBlc Select Sys.CopyVar(var1, cpy)
        up_blc_copy.VarBlc.AddRange(vvar1)

        ' up_blcの子の文をup_blc_copyにコピーする。
        up_blc_copy.StmtBlc.AddRange(From x In up_blc.StmtBlc Select CType(If(x Is if1, blc1, Sys.CopyStatement(x, cpy)), TStatement))

        If up_blc.UpTrm Is if1.FunctionTrm Then
            ' メソッドの直下のブロックの場合

            Return up_blc_copy
        Else
            ' メソッドの直下のブロックでない場合

            ' １つ上のIf文を得る。
            Dim if_blc As TIfBlock = CType(Sys.UpStmtProper(up_blc.UpTrm), TIfBlock)
            Dim if2 As TIf = CType(if_blc.UpTrm, TIf)

            ' １つ上のif文を囲むブロックをコピーする。
            Return CopyAncestorBlock(if2, up_blc_copy, cpy)
        End If
    End Function

    Public Overrides Sub StartCondition(self As Object)
        If TypeOf self Is TIfBlock Then
            With CType(self, TIfBlock)
                Dim if1 As TIf = CType(.UpTrm, TIf)
                If if1.VirtualizableIf Then

                    Dim fnc1 As New TFunction
                    Dim blc_if_copy As New TBlock
                    Dim cpy As New TCopy

                    cpy.CurFncCpy = fnc1

                    ' 関数のthisをコピーする。
                    fnc1.ThisFnc = Sys.CopyVar(.FunctionTrm.ThisFnc, cpy)

                    ' 関数の引数をコピーする。
                    Dim varg_var = From var1 In .FunctionTrm.ArgFnc Select Sys.CopyVar(var1, cpy)
                    fnc1.ArgFnc.AddRange(varg_var)

                    ' .BlcIfの変数をblc_if_copyにコピーする。
                    Dim vvar1 = From var1 In .BlcIf.VarBlc Select Sys.CopyVar(var1, cpy)
                    blc_if_copy.VarBlc.AddRange(vvar1)

                    ' このif文を囲むブロックの変数をコピーする。
                    Dim vvar2 = (From blc In Sys.AncestorList(if1) Where TypeOf blc Is TBlock From var1 In CType(blc, TBlock).VarBlc Select Sys.CopyVar(var1, cpy)).ToList()

                    ' .BlcIfの子の文をblc_if_copyにコピーする。
                    blc_if_copy.StmtBlc.AddRange(From x In .BlcIf.StmtBlc Where Not x.VirtualizableIf Select Sys.CopyStatement(x, cpy))

                    ' このif文を囲むブロックをコピーする。
                    fnc1.BlcFnc = CopyAncestorBlock(if1, blc_if_copy, cpy)

                    ' クラスにメソッドを追加する。
                    Dim app1 As TApply = CType(.CndIf, TApply)
                    Dim ref1 As TReference = CType(app1.ArgApp(1), TReference)
                    Dim virtualizable_class As TClass = CType(ref1.VarRef, TClass)

                    fnc1.NameVar = .FunctionTrm.NameVar
                    fnc1.ModVar = New TModifier()
                    fnc1.ModVar.isInvariant = True
                    fnc1.TypeFnc = .FunctionTrm.TypeFnc
                    fnc1.ClaFnc = virtualizable_class
                    fnc1.ClaFnc.FncCla.Add(fnc1)
                    fnc1.WithFnc = virtualizable_class
                    fnc1.OrgRule = OrgRuleNavi

                    Debug.Print("Make Rule {0}.{1}", virtualizable_class.NameVar, fnc1.NameVar)

                    'fnc1.__SetParent(fnc1, fnc1.ClaFnc.FncCla)
                    Dim set_parent_stmt As New TNaviSetParentStmt
                    set_parent_stmt.NaviFunction(fnc1, Nothing)

                    Dim set_up_trm As New TNaviSetUpTrm
                    set_up_trm.NaviFunction(fnc1, Nothing)

                    UDA.VirtualizedMethodList.Add(fnc1)
                End If
            End With
        End If
    End Sub
End Class
