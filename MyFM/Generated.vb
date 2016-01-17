Partial Public Class TClassStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TClassStatement)
            MyBase.__SetParent(self, _Parent)
            If .ClaClaStmt IsNot Nothing Then
                .ClaClaStmt.__SetParent(.ClaClaStmt, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TImplementsStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TImplementsStatement)
            MyBase.__SetParent(self, _Parent)
            If .ClassImplementsStmt IsNot Nothing Then
                .ClassImplementsStmt.UpList = self
                For Each x In .ClassImplementsStmt
                    x.__SetParent(x, .ClassImplementsStmt)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TFunctionStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TFunctionStatement)
            MyBase.__SetParent(self, _Parent)
            If .ModifierFncStmt IsNot Nothing Then
                .ModifierFncStmt.__SetParent(.ModifierFncStmt, self)
            End If
            If .ArgumentClassFncStmt IsNot Nothing Then
                .ArgumentClassFncStmt.UpList = self
                For Each x In .ArgumentClassFncStmt
                    x.__SetParent(x, .ArgumentClassFncStmt)
                Next
            End If
            If .ArgumentFncStmt IsNot Nothing Then
                .ArgumentFncStmt.UpList = self
                For Each x In .ArgumentFncStmt
                    x.__SetParent(x, .ArgumentFncStmt)
                Next
            End If
            If .RetType IsNot Nothing Then
                .RetType.__SetParent(.RetType, self)
            End If
            If .InterfaceFncStmt IsNot Nothing Then
                .InterfaceFncStmt.__SetParent(.InterfaceFncStmt, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TIfStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TIfStatement)
            MyBase.__SetParent(self, _Parent)
            If .CndIfStmt IsNot Nothing Then
                .CndIfStmt.__SetParent(.CndIfStmt, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TSelectStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TSelectStatement)
            MyBase.__SetParent(self, _Parent)
            If .TermSelectStatement IsNot Nothing Then
                .TermSelectStatement.__SetParent(.TermSelectStatement, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TCaseStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TCaseStatement)
            MyBase.__SetParent(self, _Parent)
            If .TermCaseStmt IsNot Nothing Then
                .TermCaseStmt.UpList = self
                For Each x In .TermCaseStmt
                    x.__SetParent(x, .TermCaseStmt)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TCatchStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TCatchStatement)
            MyBase.__SetParent(self, _Parent)
            If .VariableCatchStmt IsNot Nothing Then
                .VariableCatchStmt.__SetParent(.VariableCatchStmt, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TForStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TForStatement)
            MyBase.__SetParent(self, _Parent)
            If .IdxForStmt IsNot Nothing Then
                .IdxForStmt.__SetParent(.IdxForStmt, self)
            End If
            If .FromForStmt IsNot Nothing Then
                .FromForStmt.__SetParent(.FromForStmt, self)
            End If
            If .ToForStmt IsNot Nothing Then
                .ToForStmt.__SetParent(.ToForStmt, self)
            End If
            If .StepForStmt IsNot Nothing Then
                .StepForStmt.__SetParent(.StepForStmt, self)
            End If
            If .InVarForStmt IsNot Nothing Then
                .InVarForStmt.__SetParent(.InVarForStmt, self)
            End If
            If .InTrmForStmt IsNot Nothing Then
                .InTrmForStmt.__SetParent(.InTrmForStmt, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TThrow
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TThrow)
            MyBase.__SetParent(self, _Parent)
            If .TrmThrow IsNot Nothing Then
                .TrmThrow.__SetParent(.TrmThrow, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TReDim
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TReDim)
            MyBase.__SetParent(self, _Parent)
            If .TrmReDim IsNot Nothing Then
                .TrmReDim.__SetParent(.TrmReDim, self)
            End If
            If .DimReDim IsNot Nothing Then
                .DimReDim.UpList = self
                For Each x In .DimReDim
                    x.__SetParent(x, .DimReDim)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TElseIf
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TElseIf)
            MyBase.__SetParent(self, _Parent)
            If .CndElseIf IsNot Nothing Then
                .CndElseIf.__SetParent(.CndElseIf, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TDoStmt
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TDoStmt)
            MyBase.__SetParent(self, _Parent)
            If .CndDo IsNot Nothing Then
                .CndDo.__SetParent(.CndDo, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TModifier
    Public Overridable Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TModifier)
        End With
    End Sub

End Class

Partial Public Class TTerm
    Public Overridable Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TTerm)
            .UpTrm = _Parent
        End With
    End Sub

End Class

Partial Public Class TVariable
    Public Overridable Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TVariable)
            .UpVar = _Parent
            If .ModVar IsNot Nothing Then
                .ModVar.__SetParent(.ModVar, self)
            End If
            If .InitVar IsNot Nothing Then
                .InitVar.__SetParent(.InitVar, self)
            End If
            If .ComVar IsNot Nothing Then
                .ComVar.__SetParent(.ComVar, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TClass
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TClass)
            MyBase.__SetParent(self, _Parent)
            If .FldCla IsNot Nothing Then
                .FldCla.UpList = self
                For Each x In .FldCla
                    x.__SetParent(x, .FldCla)
                Next
            End If
            If .FncCla IsNot Nothing Then
                .FncCla.UpList = self
                For Each x In .FncCla
                    x.__SetParent(x, .FncCla)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TDelegate
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TDelegate)
            MyBase.__SetParent(self, _Parent)
            If .ArgDlg IsNot Nothing Then
                .ArgDlg.UpList = self
                For Each x In .ArgDlg
                    x.__SetParent(x, .ArgDlg)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TFunction
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TFunction)
            MyBase.__SetParent(self, _Parent)
            If .ArgFnc IsNot Nothing Then
                .ArgFnc.UpList = self
                For Each x In .ArgFnc
                    x.__SetParent(x, .ArgFnc)
                Next
            End If
            If .ThisFnc IsNot Nothing Then
                .ThisFnc.__SetParent(.ThisFnc, self)
            End If
            If .BlcFnc IsNot Nothing Then
                .BlcFnc.__SetParent(.BlcFnc, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TParenthesis
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TParenthesis)
            MyBase.__SetParent(self, _Parent)
            If .TrmPar IsNot Nothing Then
                .TrmPar.__SetParent(.TrmPar, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TApply
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TApply)
            MyBase.__SetParent(self, _Parent)
            If .ArgApp IsNot Nothing Then
                .ArgApp.UpList = self
                For Each x In .ArgApp
                    x.__SetParent(x, .ArgApp)
                Next
            End If
            If .FncApp IsNot Nothing Then
                .FncApp.__SetParent(.FncApp, self)
            End If
            If .IniApp IsNot Nothing Then
                .IniApp.__SetParent(.IniApp, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TDot
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TDot)
            MyBase.__SetParent(self, _Parent)
            If .TrmDot IsNot Nothing Then
                .TrmDot.__SetParent(.TrmDot, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TQuery
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TQuery)
            MyBase.__SetParent(self, _Parent)
            If .SeqQry IsNot Nothing Then
                .SeqQry.__SetParent(.SeqQry, self)
            End If
            If .VarQry IsNot Nothing Then
                .VarQry.__SetParent(.VarQry, self)
            End If
            If .CndQry IsNot Nothing Then
                .CndQry.__SetParent(.CndQry, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TFrom
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TFrom)
            MyBase.__SetParent(self, _Parent)
            If .SelFrom IsNot Nothing Then
                .SelFrom.__SetParent(.SelFrom, self)
            End If
            If .TakeFrom IsNot Nothing Then
                .TakeFrom.__SetParent(.TakeFrom, self)
            End If
            If .InnerFrom IsNot Nothing Then
                .InnerFrom.__SetParent(.InnerFrom, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TAggregate
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TAggregate)
            MyBase.__SetParent(self, _Parent)
            If .IntoAggr IsNot Nothing Then
                .IntoAggr.__SetParent(.IntoAggr, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TArray
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TArray)
            MyBase.__SetParent(self, _Parent)
            If .TrmArr IsNot Nothing Then
                .TrmArr.UpList = self
                For Each x In .TrmArr
                    x.__SetParent(x, .TrmArr)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TStatement
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TStatement)
            .UpTrm = _Parent
        End With
    End Sub

End Class

Partial Public Class TVariableDeclaration
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TVariableDeclaration)
            MyBase.__SetParent(self, _Parent)
            If .ModDecl IsNot Nothing Then
                .ModDecl.__SetParent(.ModDecl, self)
            End If
            If .VarDecl IsNot Nothing Then
                .VarDecl.UpList = self
                For Each x In .VarDecl
                    x.__SetParent(x, .VarDecl)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TBlock
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TBlock)
            MyBase.__SetParent(self, _Parent)
            If .StmtBlc IsNot Nothing Then
                .StmtBlc.UpList = self
                For Each x In .StmtBlc
                    x.__SetParent(x, .StmtBlc)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TIf
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TIf)
            MyBase.__SetParent(self, _Parent)
            If .IfBlc IsNot Nothing Then
                .IfBlc.UpList = self
                For Each x In .IfBlc
                    x.__SetParent(x, .IfBlc)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TIfBlock
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TIfBlock)
            MyBase.__SetParent(self, _Parent)
            If .CndIf IsNot Nothing Then
                .CndIf.__SetParent(.CndIf, self)
            End If
            If .WithIf IsNot Nothing Then
                .WithIf.__SetParent(.WithIf, self)
            End If
            If .BlcIf IsNot Nothing Then
                .BlcIf.__SetParent(.BlcIf, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TCase
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TCase)
            MyBase.__SetParent(self, _Parent)
            If .TrmCase IsNot Nothing Then
                .TrmCase.UpList = self
                For Each x In .TrmCase
                    x.__SetParent(x, .TrmCase)
                Next
            End If
            If .BlcCase IsNot Nothing Then
                .BlcCase.__SetParent(.BlcCase, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TSelect
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TSelect)
            MyBase.__SetParent(self, _Parent)
            If .TrmSel IsNot Nothing Then
                .TrmSel.__SetParent(.TrmSel, self)
            End If
            If .CaseSel IsNot Nothing Then
                .CaseSel.UpList = self
                For Each x In .CaseSel
                    x.__SetParent(x, .CaseSel)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class TTry
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TTry)
            MyBase.__SetParent(self, _Parent)
            If .BlcTry IsNot Nothing Then
                .BlcTry.__SetParent(.BlcTry, self)
            End If
            If .VarCatch IsNot Nothing Then
                .VarCatch.UpList = self
                For Each x In .VarCatch
                    x.__SetParent(x, .VarCatch)
                Next
            End If
            If .BlcCatch IsNot Nothing Then
                .BlcCatch.__SetParent(.BlcCatch, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TFor
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TFor)
            MyBase.__SetParent(self, _Parent)
            If .IdxVarFor IsNot Nothing Then
                .IdxVarFor.__SetParent(.IdxVarFor, self)
            End If
            If .InTrmFor IsNot Nothing Then
                .InTrmFor.__SetParent(.InTrmFor, self)
            End If
            If .InVarFor IsNot Nothing Then
                .InVarFor.__SetParent(.InVarFor, self)
            End If
            If .IdxFor IsNot Nothing Then
                .IdxFor.__SetParent(.IdxFor, self)
            End If
            If .FromFor IsNot Nothing Then
                .FromFor.__SetParent(.FromFor, self)
            End If
            If .ToFor IsNot Nothing Then
                .ToFor.__SetParent(.ToFor, self)
            End If
            If .StepFor IsNot Nothing Then
                .StepFor.__SetParent(.StepFor, self)
            End If
            If .IniFor IsNot Nothing Then
                .IniFor.__SetParent(.IniFor, self)
            End If
            If .CndFor IsNot Nothing Then
                .CndFor.__SetParent(.CndFor, self)
            End If
            If .StepStmtFor IsNot Nothing Then
                .StepStmtFor.__SetParent(.StepStmtFor, self)
            End If
            If .BlcFor IsNot Nothing Then
                .BlcFor.__SetParent(.BlcFor, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TAssignment
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TAssignment)
            MyBase.__SetParent(self, _Parent)
            If .RelAsn IsNot Nothing Then
                .RelAsn.__SetParent(.RelAsn, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TCall
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TCall)
            MyBase.__SetParent(self, _Parent)
            If .AppCall IsNot Nothing Then
                .AppCall.__SetParent(.AppCall, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TReturn
    Public Overrides Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TReturn)
            MyBase.__SetParent(self, _Parent)
            If .TrmRet IsNot Nothing Then
                .TrmRet.__SetParent(.TrmRet, self)
            End If
        End With
    End Sub

End Class

Partial Public Class TProject
    Public Overridable Sub __SetParent(self As Object, _Parent As Object)
        With CType(self, TProject)
            If .SimpleParameterizedClassList IsNot Nothing Then
                .SimpleParameterizedClassList.UpList = self
                For Each x In .SimpleParameterizedClassList
                    x.__SetParent(x, .SimpleParameterizedClassList)
                Next
            End If
        End With
    End Sub

End Class

Partial Public Class Sys
    Public Shared Sub SetParent(self As Object, _Parent As Object)
        If TypeOf self Is TStatement Then
            CType(self, TStatement).__SetParent(self, _Parent)
        ElseIf TypeOf self Is TTerm Then
            CType(self, TTerm).__SetParent(self, _Parent)
        Else
            Debug.Assert(False)
        End If
    End Sub
End Class

