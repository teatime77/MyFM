﻿Imports System.Diagnostics

' -------------------------------------------------------------------------------- TNavi
Public Class TNavi
    Public RefCnt As Integer
    Public ErrNav As Boolean = False

    Public Sub IncRefCnt(ref1 As TReference)
        RefCnt += 1
    End Sub


    Public Overridable Function StartReference(ref1 As TReference, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartDot(dot1 As TDot, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartLocalVariable(var1 As TVariable, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartTerm(trm1 As TTerm, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartCase(case1 As TCase, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartAssignment(asn1 As TAssignment, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartIf(if1 As TIf, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartFunction(fnc1 As TFunction, arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartLocalVariableList(list1 As TList(Of TVariable), arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartTermList(list1 As TList(Of TTerm), arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartCaseList(list1 As TList(Of TCase), arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartStatementList(list1 As TList(Of TStatement), arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartBlockList(list1 As TList(Of TBlock), arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Function StartIfBlockList(list1 As TList(Of TIfBlock), arg1 As Object) As Object
        Return arg1
    End Function

    Public Overridable Sub EndDot(dot1 As TDot, arg1 As Object)
    End Sub

    Public Overridable Sub EndApply(app1 As TApply, arg1 As Object)
    End Sub

    Public Overridable Sub EndLocalVariable(var1 As TVariable, arg1 As Object)
    End Sub

    Public Overridable Sub EndStatement(stmt1 As TStatement, arg1 As Object)
    End Sub

    Public Overridable Sub NaviDot(dot1 As TDot, arg1 As Object)
        arg1 = StartDot(dot1, arg1)

        NaviTerm(dot1.TrmDot, arg1)

        EndDot(dot1, arg1)
    End Sub

    Public Overridable Sub NaviReference(ref1 As TReference, arg1 As Object)
        StartReference(ref1, arg1)
    End Sub

    Public Overridable Sub NaviTerm(trm1 As TTerm, arg1 As Object)
        If trm1 IsNot Nothing Then

            arg1 = StartTerm(trm1, arg1)

            Try
                If TypeOf trm1 Is TConstant Then
                ElseIf TypeOf trm1 Is TArray Then
                    NaviArray(CType(trm1, TArray), arg1)
                ElseIf TypeOf trm1 Is TDot Then
                    NaviDot(CType(trm1, TDot), arg1)
                ElseIf TypeOf trm1 Is TReference Then
                    NaviReference(CType(trm1, TReference), arg1)
                ElseIf trm1.IsApp() Then
                    NaviApply(CType(trm1, TApply), arg1)
                ElseIf trm1.IsLog() Then
                    NaviLog(CType(trm1, TApply), arg1)
                ElseIf TypeOf trm1 Is TParenthesis Then
                    NaviTerm(CType(trm1, TParenthesis).TrmPar, arg1)
                ElseIf TypeOf trm1 Is TFrom Then
                    NaviFrom(CType(trm1, TFrom), arg1)
                ElseIf TypeOf trm1 Is TAggregate Then
                    NaviAggregate(CType(trm1, TAggregate), arg1)
                Else
                    Debug.Assert(False)
                End If
            Catch ex As TError
                ErrNav = True
            End Try
        End If
    End Sub

    Public Overridable Sub NaviTermList(list1 As TList(Of TTerm), arg1 As Object)
        arg1 = StartTermList(list1, arg1)

        For Each trm1 In list1
            NaviTerm(trm1, arg1)
        Next
    End Sub

    Public Overridable Sub NaviCaseList(list1 As TList(Of TCase), arg1 As Object)
        arg1 = StartCaseList(list1, arg1)

        For Each cas1 In list1
            NaviStatement(cas1, arg1)
        Next
    End Sub

    Public Overridable Sub NaviStatementList(list1 As TList(Of TStatement), arg1 As Object)
        arg1 = StartStatementList(list1, arg1)

        For Each stmt1 In list1
            NaviStatement(stmt1, arg1)
        Next
    End Sub

    Public Overridable Sub NaviBlockList(list1 As TList(Of TBlock), arg1 As Object)
        arg1 = StartBlockList(list1, arg1)

        For Each blc_f In list1
            NaviStatement(blc_f, arg1)
        Next
    End Sub

    Public Overridable Sub NaviIfBlockList(list1 As TList(Of TIfBlock), arg1 As Object)
        arg1 = StartIfBlockList(list1, arg1)

        For Each blc_f In list1
            NaviStatement(blc_f, arg1)
        Next
    End Sub

    Public Overridable Sub NaviLocalVariableList(list1 As TList(Of TVariable), arg1 As Object)
        arg1 = StartLocalVariableList(list1, arg1)

        For Each var1 In list1
            NaviLocalVariable(var1, arg1)
        Next
    End Sub

    Public Overridable Sub NaviLocalVariable(var1 As TVariable, arg1 As Object)
        If var1 IsNot Nothing Then

            arg1 = StartLocalVariable(var1, arg1)

            NaviTerm(var1.InitVar, arg1)

            EndLocalVariable(var1, arg1)
        End If
    End Sub

    Public Overridable Sub NaviArray(arr1 As TArray, arg1 As Object)
        If arr1 IsNot Nothing Then
            NaviTermList(arr1.TrmArr, arg1)
        End If
    End Sub

    Public Overridable Sub NaviApply(app1 As TApply, arg1 As Object)
        If app1 IsNot Nothing Then
            NaviTermList(app1.ArgApp, arg1)

            If app1.FncApp IsNot Nothing Then
                NaviTerm(app1.FncApp, arg1)
            End If

            If app1.IniApp IsNot Nothing Then
                NaviTerm(app1.IniApp, arg1)
            End If

            EndApply(app1, arg1)
        End If
    End Sub

    Public Overridable Sub NaviLog(opr1 As TApply, arg1 As Object)
        If opr1 IsNot Nothing Then
            NaviTermList(opr1.ArgApp, arg1)
        End If
    End Sub

    Public Overridable Sub NaviFrom(frm1 As TFrom, arg1 As Object)
        NaviTerm(frm1.SeqQry, arg1)
        NaviLocalVariable(frm1.VarQry, arg1)
        NaviTerm(frm1.CndQry, arg1)
        NaviTerm(frm1.SelFrom, arg1)
        NaviTerm(frm1.TakeFrom, arg1)
        NaviTerm(frm1.InnerFrom, arg1)
    End Sub

    Public Overridable Sub NaviAggregate(aggr1 As TAggregate, arg1 As Object)
        NaviTerm(aggr1.SeqQry, arg1)
        NaviLocalVariable(aggr1.VarQry, arg1)
        NaviTerm(aggr1.CndQry, arg1)
        NaviTerm(aggr1.IntoAggr, arg1)
    End Sub

    Public Overridable Sub NaviFor(for1 As TFor, arg1 As Object)
        NaviTerm(for1.IdxFor, arg1)
        NaviTerm(for1.InTrmFor, arg1)
        NaviLocalVariable(for1.InVarFor, arg1)
        NaviTerm(for1.FromFor, arg1)
        NaviTerm(for1.ToFor, arg1)
        NaviTerm(for1.StepFor, arg1)
        NaviStatement(for1.IniFor, arg1)
        NaviTerm(for1.CndFor, arg1)
        NaviStatement(for1.StepStmtFor, arg1)
        NaviStatement(for1.BlcFor, arg1)
    End Sub

    Public Overridable Sub NaviCase(cas1 As TCase, arg1 As Object)
        arg1 = StartCase(cas1, arg1)

        NaviTermList(cas1.TrmCase, arg1)
        NaviStatement(cas1.BlcCase, arg1)
    End Sub

    Public Overridable Sub NaviSelect(swt1 As TSelect, arg1 As Object)
        NaviTerm(swt1.TrmSel, arg1)
        NaviCaseList(swt1.CaseSel, arg1)
    End Sub

    Public Overridable Sub NaviAssignment(asn1 As TAssignment, arg1 As Object)
        arg1 = StartAssignment(asn1, arg1)
        NaviTerm(asn1.RelAsn, arg1)
    End Sub

    Public Overridable Sub NaviCall(call1 As TCall, arg1 As Object)
        NaviTerm(call1.AppCall, arg1)
    End Sub

    Public Overridable Sub NaviReDim(red1 As TReDim, arg1 As Object)
        NaviTerm(red1.TrmReDim, arg1)
        NaviTermList(red1.DimReDim, arg1)
    End Sub

    Public Overridable Sub NaviIf(if1 As TIf, arg1 As Object)
        arg1 = StartIf(if1, arg1)
        NaviIfBlockList(if1.IfBlc, arg1)
    End Sub

    Public Overridable Sub NaviIfBlock(if_blc As TIfBlock, arg1 As Object)
        NaviTerm(if_blc.CndIf, arg1)
        NaviTerm(if_blc.WithIf, arg1)
        NaviStatement(if_blc.BlcIf, arg1)
    End Sub

    Public Overridable Sub NaviTry(try1 As TTry, arg1 As Object)
        NaviStatement(try1.BlcTry, arg1)
        NaviLocalVariableList(try1.VarCatch, arg1)
        NaviStatement(try1.BlcCatch, arg1)
    End Sub

    Public Overridable Sub NaviVariableDeclaration(dcl1 As TVariableDeclaration, arg1 As Object)
        NaviLocalVariableList(dcl1.VarDecl, arg1)
    End Sub

    Public Overridable Sub NaviReturn(ret1 As TReturn, arg1 As Object)
        NaviTerm(ret1.TrmRet, arg1)
    End Sub

    Public Overridable Sub NaviThrow(throw1 As TThrow, arg1 As Object)
        NaviTerm(throw1.TrmThrow, arg1)
    End Sub

    Public Overridable Sub NaviBlock(blc1 As TBlock, arg1 As Object)
        NaviStatementList(blc1.StmtBlc, arg1)
    End Sub

    Public Overridable Sub NaviStatement(stmt1 As TStatement, arg1 As Object)

        arg1 = StartStatement(stmt1, arg1)

        If stmt1 IsNot Nothing Then

            If TypeOf stmt1 Is TAssignment Then
                NaviAssignment(CType(stmt1, TAssignment), arg1)
            ElseIf TypeOf stmt1 Is TCall Then
                NaviCall(CType(stmt1, TCall), arg1)
            ElseIf TypeOf stmt1 Is TVariableDeclaration Then
                NaviVariableDeclaration(CType(stmt1, TVariableDeclaration), arg1)
            ElseIf TypeOf stmt1 Is TReDim Then
                NaviReDim(CType(stmt1, TReDim), arg1)
            ElseIf TypeOf stmt1 Is TIf Then
                NaviIf(CType(stmt1, TIf), arg1)
            ElseIf TypeOf stmt1 Is TIfBlock Then
                NaviIfBlock(CType(stmt1, TIfBlock), arg1)
            ElseIf TypeOf stmt1 Is TSelect Then
                NaviSelect(CType(stmt1, TSelect), arg1)
            ElseIf TypeOf stmt1 Is TCase Then
                NaviCase(CType(stmt1, TCase), arg1)
            ElseIf TypeOf stmt1 Is TTry Then
                NaviTry(CType(stmt1, TTry), arg1)
            ElseIf TypeOf stmt1 Is TFor Then
                NaviFor(CType(stmt1, TFor), arg1)
            ElseIf TypeOf stmt1 Is TBlock Then
                NaviBlock(CType(stmt1, TBlock), arg1)
            ElseIf TypeOf stmt1 Is TReturn Then
                NaviReturn(CType(stmt1, TReturn), arg1)
            ElseIf TypeOf stmt1 Is TThrow Then
                NaviThrow(CType(stmt1, TThrow), arg1)
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

        EndStatement(stmt1, arg1)
    End Sub

    Public Overridable Sub NaviFunction(fnc1 As TFunction, arg1 As Object)
        arg1 = StartFunction(fnc1, arg1)

        NaviStatement(fnc1.ComVar, arg1)
        NaviLocalVariableList(fnc1.ArgFnc, arg1)
        If fnc1.BlcFnc IsNot Nothing Then
            NaviStatement(fnc1.BlcFnc, arg1)
        End If
    End Sub

    Public Overridable Sub NaviClass(cla1 As TClass, arg1 As Object)
        If (cla1.FldCla.Count <> 0 OrElse cla1.FncCla.Count <> 0) AndAlso Not (cla1.GenCla IsNot Nothing AndAlso cla1.OrgCla Is Nothing) Then

            '  すべてのメソッドに対し
            For Each fnc1 In cla1.FncCla
                NaviFunction(fnc1, arg1)
            Next
        End If
    End Sub

    Public Overridable Sub NaviProject(prj As TProject, arg1 As Object)
        '  すべてのクラスに対し
        For Each cla1 In prj.SimpleParameterizedClassList
            NaviClass(cla1, arg1)
        Next
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviTest
Public Class TNaviTest
    Inherits TNavi

    Public Overrides Sub NaviDot(dot1 As TDot, arg1 As Object)
        IncRefCnt(dot1)
        If dot1.TrmDot Is Nothing Then
        Else
            NaviTerm(dot1.TrmDot, arg1)
        End If
    End Sub

    Public Overrides Sub NaviReference(ref1 As TReference, arg1 As Object)
        IncRefCnt(ref1)
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetDefRef
Public Class TNaviSetDefRef
    Inherits TNavi

    Public Overrides Function StartAssignment(asn1 As TAssignment, arg1 As Object) As Object
        If TypeOf asn1.RelAsn.ArgApp(0) Is TReference Then
            ' 左辺が変数参照の場合

            CType(asn1.RelAsn.ArgApp(0), TReference).DefRef = True
        Else
            ' 左辺が関数呼び出しの場合

            Debug.Assert(asn1.RelAsn.ArgApp(0).IsApp())
            Dim app1 As TApply = CType(asn1.RelAsn.ArgApp(0), TApply)

            Debug.Assert(app1.KndApp = EApply.ArrayApp OrElse app1.KndApp = EApply.ListApp)
            Debug.Assert(TypeOf app1.FncApp Is TReference)

            CType(app1.FncApp, TReference).DefRef = True
        End If

        Return arg1
    End Function
End Class


' -------------------------------------------------------------------------------- TNaviSetVarRef
Public Class TNaviSetVarRef
    Inherits TNavi

    Public Overrides Sub NaviDot(dot1 As TDot, arg1 As Object)
        IncRefCnt(dot1)

        Debug.Assert(dot1.VarRef IsNot Nothing)
        Debug.Assert(Not dot1.VarRef.RefVar.Contains(dot1))

        dot1.VarRef.RefVar.Add(dot1)

        NaviTerm(dot1.TrmDot, arg1)
    End Sub

    Public Overrides Sub NaviReference(ref1 As TReference, arg1 As Object)
        IncRefCnt(ref1)

        Debug.Assert(ref1.VarRef IsNot Nothing)
        Debug.Assert(Not ref1.VarRef.RefVar.Contains(ref1))

        ref1.VarRef.RefVar.Add(ref1)
    End Sub
End Class


' -------------------------------------------------------------------------------- TNaviSetCall
Public Class TNaviSetCall
    Inherits TNavi

    Public Sub SetCall(ref1 As TReference)
        Dim fnc2 As TFunction

        fnc2 = CType(ref1.VarRef, TFunction)

        If Not ref1.FunctionTrm.CallTo.Contains(fnc2) Then
            ref1.FunctionTrm.CallTo.Add(fnc2)
        End If
        If Not fnc2.CallFrom.Contains(ref1.FunctionTrm) Then
            fnc2.CallFrom.Add(ref1.FunctionTrm)
        End If
    End Sub

    Public Overrides Sub NaviDot(dot1 As TDot, arg1 As Object)
        IncRefCnt(dot1)

        If dot1.VarRef IsNot Nothing AndAlso TypeOf dot1.VarRef Is TFunction Then

            SetCall(dot1)
        End If

        If dot1.TrmDot Is Nothing Then
        Else
            NaviTerm(dot1.TrmDot, arg1)
        End If
    End Sub

    Public Overrides Sub NaviReference(ref1 As TReference, arg1 As Object)
        IncRefCnt(ref1)
        If ref1.VarRef IsNot Nothing AndAlso TypeOf ref1.VarRef Is TFunction Then

            SetCall(ref1)
        End If
    End Sub
End Class

' -------------------------------------------------------------------------------- TNaviSetFunction
Public Class TNaviSetFunction
    Inherits TNavi
    Public FunctionNavi As TFunction

    Public Overrides Function StartTerm(trm1 As TTerm, arg1 As Object) As Object
        If trm1 IsNot Nothing Then
            trm1.FunctionTrm = FunctionNavi
            If TypeOf trm1 Is TReference Then
                trm1.FunctionTrm.RefFnc.Add(CType(trm1, TReference))
            End If
        End If

        Return arg1
    End Function

    Public Overrides Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        If stmt1 IsNot Nothing Then
            stmt1.FunctionTrm = FunctionNavi
        End If

        Return arg1
    End Function

    Public Overrides Function StartFunction(fnc1 As TFunction, arg1 As Object) As Object
        FunctionNavi = fnc1

        Return arg1
    End Function
End Class

' -------------------------------------------------------------------------------- TNaviSetParentStmt
Public Class TNaviSetParentStmt
    Inherits TNavi

    Public Overrides Function StartLocalVariable(var1 As TVariable, arg1 As Object) As Object
        If var1 IsNot Nothing Then
            'Debug.Assert(var1.UpVar Is arg1)
            var1.UpVar = arg1
        End If

        Return var1
    End Function

    Public Overrides Function StartTerm(trm1 As TTerm, arg1 As Object) As Object
        If trm1 IsNot Nothing Then
            'Debug.Assert(trm1.UpTrm Is arg1)
            trm1.UpTrm = arg1
        End If

        Return trm1
    End Function

    Public Overrides Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        If stmt1 IsNot Nothing Then
            If TypeOf stmt1 Is TIfBlock Then
                'Debug.Assert(stmt1.UpTrm Is CType(arg1, TIf).IfBlc)
            Else
                'Debug.Assert(stmt1.UpTrm Is arg1)
            End If
            stmt1.UpTrm = arg1
        End If

        Return stmt1
    End Function

    Public Overrides Function StartIf(if1 As TIf, arg1 As Object) As Object
        If if1 IsNot Nothing Then
            'Debug.Assert(if1.IfBlc.UpList Is if1)
            if1.IfBlc.UpList = if1
        End If

        Return if1
    End Function

    Public Overrides Function StartFunction(fnc1 As TFunction, arg1 As Object) As Object
        Return fnc1
    End Function

    Public Overrides Function StartLocalVariableList(list1 As TList(Of TVariable), arg1 As Object) As Object
        If list1 IsNot Nothing Then
            'Debug.Assert(list1.UpList Is arg1)
            list1.UpList = arg1
        End If

        Return list1
    End Function

    Public Overrides Function StartTermList(list1 As TList(Of TTerm), arg1 As Object) As Object
        If list1 IsNot Nothing Then
            'Debug.Assert(list1.UpList Is arg1)
            list1.UpList = arg1
        End If

        Return list1
    End Function

    Public Overrides Function StartCaseList(list1 As TList(Of TCase), arg1 As Object) As Object
        If list1 IsNot Nothing Then
            'Debug.Assert(list1.UpList Is arg1)
            list1.UpList = arg1
        End If

        Return list1
    End Function

    Public Overrides Function StartStatementList(list1 As TList(Of TStatement), arg1 As Object) As Object
        If list1 IsNot Nothing Then
            'Debug.Assert(list1.UpList Is arg1)
            list1.UpList = arg1
        End If

        Return list1
    End Function

    Public Overrides Function StartBlockList(list1 As TList(Of TBlock), arg1 As Object) As Object
        If list1 IsNot Nothing Then
            'Debug.Assert(list1.UpList Is arg1)
            list1.UpList = arg1
        End If

        Return list1
    End Function
End Class


' -------------------------------------------------------------------------------- TNaviSetUpTrm
Public Class TNaviSetUpTrm
    Inherits TNavi
    Public NavUp As New Sys

    Public Sub Test(ref1 As TReference)
        Dim stmt1 As TStatement

        NavUp.UpToFnc(ref1)

        stmt1 = Sys.UpToStmt(ref1)
    End Sub

    Public Overrides Function StartReference(ref1 As TReference, arg1 As Object) As Object
        Test(ref1)
        Return arg1
    End Function

    Public Overrides Function StartDot(dot1 As TDot, arg1 As Object) As Object
        Test(dot1)
        Return arg1
    End Function

End Class

' -------------------------------------------------------------------------------- TNaviClearUsedStmt
Public Class TNaviClearUsedStmt
    Inherits TNavi

    Public Overrides Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        stmt1.UsedStmt = False
        Return arg1
    End Function

End Class

' -------------------------------------------------------------------------------- TNaviAllStmt
Public Class TNaviAllStmt
    Inherits TNavi
    Public AllStmts As New TList(Of TStatement)

    Public Overrides Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        If stmt1 IsNot Nothing Then
            AllStmts.Add(stmt1)
        End If

        Return arg1
    End Function
End Class

' -------------------------------------------------------------------------------- TNaviSetValidStmt
Public Class TNaviSetValidStmt
    Inherits TNavi

    Public Overrides Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        If stmt1 IsNot Nothing Then

            Dim up_stmt As TStatement = Sys.UpStmtProper(stmt1.UpTrm)

            If up_stmt IsNot Nothing AndAlso Not up_stmt.ValidStmt Then
                ' 親の文が無効の場合

                ' この文も無効とする。
                stmt1.ValidStmt = False
            End If
        End If

        Return arg1
    End Function
End Class

' -------------------------------------------------------------------------------- TNaviSetValidStmt
Public Class TNaviMakeVirtualMethod
    Inherits TNavi
    Public InnermostClass As TClass
    Public VirtualSeparable As Boolean
    Public CurrentVar As TVariable
    Public StmtToInnermostClass As New Dictionary(Of TStatement, TClass)

    Public Overrides Function StartStatement(stmt1 As TStatement, arg1 As Object) As Object
        If stmt1 IsNot Nothing Then
            StmtToInnermostClass.Add(stmt1, InnermostClass)
        End If

        Return stmt1
    End Function

    Public Overrides Sub NaviIfBlock(if_blc As TIfBlock, arg1 As Object)
        Dim innermost_class As TClass = InnermostClass

        If VirtualSeparable Then
            ' 仮想関数に分離可能の場合

            If TypeOf (if_blc.CndIf) Is TApply Then
                Dim app1 As TApply = CType(if_blc.CndIf, TApply)

                If app1.TypeApp = EToken.Instanceof AndAlso TypeOf app1.ArgApp(0) Is TReference Then
                    Dim ref1 As TReference = CType(app1.ArgApp(0), TReference)

                    If ref1.VarRef Is CurrentVar Then
                        InnermostClass = CType(CType(app1.ArgApp(1), TReference).VarRef, TClass)
                    End If

                End If
            End If
        End If
        NaviTerm(if_blc.CndIf, arg1)
        NaviStatement(if_blc.BlcIf, arg1)

        InnermostClass = innermost_class
    End Sub
End Class
