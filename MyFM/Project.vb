Imports System.IO
Imports System.Xml.Serialization
Imports System.Text
Imports System.Diagnostics

Public Class _Weak
    Inherits Attribute

    Public Sub New()
    End Sub
End Class

Public Class _Invariant
    Inherits Attribute

    Public Sub New()
    End Sub
End Class

Public Class _Parent
    Inherits Attribute

    Public Sub New()
    End Sub
End Class

Public Class TLibrary
    Public LibraryDirectory As String
    Public SourceFileNameList As String()
End Class

' -------------------------------------------------------------------------------- TProject
Public Class TProject
    <XmlIgnoreAttribute(), _Weak()> Public Shared Prj As TProject

    Public Language As ELanguage = ELanguage.Basic
    Public OutputDirectory As String
    Public MainClassName As String
    Public MainFunctionName As String
    Public OutputNotUsed As Boolean = True
    Public UseReferenceGraph As Boolean = False
    Public ClassNameTablePath As String = ""
    Public Dataflow As Boolean = False

    <_Weak()> Public OutputLanguageList As New List(Of ELanguage)
    <_Weak()> Public LibraryList As TLibrary()

    <XmlIgnoreAttribute()> Public SimpleParameterizedClassList As New TList(Of TClass)

    <XmlIgnoreAttribute(), _Weak()> Public ProjectHome As String
    <XmlIgnoreAttribute(), _Weak()> Public ApplicationClassList As TList(Of TClass)
    <XmlIgnoreAttribute(), _Weak()> Public SrcPrj As New TList(Of TSourceFile)
    <XmlIgnoreAttribute(), _Weak()> Public ClassNameTable As Dictionary(Of String, String)
    <XmlIgnoreAttribute(), _Weak()> Public SpecializedClassList As New TList(Of TClass)
    <XmlIgnoreAttribute(), _Weak()> Public PendingSpecializedClassList As New TList(Of TClass)
    <XmlIgnoreAttribute(), _Weak()> Public SimpleParameterizedSpecializedClassList As New TList(Of TClass)
    <XmlIgnoreAttribute(), _Weak()> Public SimpleFieldList As List(Of TField)    ' 単純クラスのフィールドのリスト
    <XmlIgnoreAttribute(), _Weak()> Public AppClassList As TList(Of TClass)
    <XmlIgnoreAttribute(), _Weak()> Public AppFieldList As TList(Of TField)
    <XmlIgnoreAttribute(), _Weak()> Public AppStrongFieldList As TList(Of TField)
    <XmlIgnoreAttribute(), _Weak()> Public vAllFnc As New TList(Of TFunction)
    <XmlIgnoreAttribute(), _Weak()> Public vAllFld As New TList(Of TField)    ' すべてのフィールド
    <XmlIgnoreAttribute(), _Weak()> Public CurSrc As TSourceFile ' 現在のソース
    <XmlIgnoreAttribute(), _Weak()> Public TypeType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public SystemType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public BoolType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public ObjectType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public DoubleType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public CharType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public IntType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public StringType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public WaitHandleType As TClass
    <XmlIgnoreAttribute(), _Weak()> Public MainClass As TClass
    <XmlIgnoreAttribute(), _Weak()> Public SimpleParameterizedClassTable As New Dictionary(Of String, TClass) ' クラス辞書
    <XmlIgnoreAttribute(), _Weak()> Public dicGenCla As New Dictionary(Of String, TClass)
    <XmlIgnoreAttribute(), _Weak()> Public dicCmpCla As New Dictionary(Of TClass, TList(Of TClass))
    <XmlIgnoreAttribute(), _Weak()> Public dicArrCla As New Dictionary(Of TClass, TList(Of TClass))
    <XmlIgnoreAttribute(), _Weak()> Public dicMemName As Dictionary(Of String, Dictionary(Of String, String))
    <XmlIgnoreAttribute(), _Weak()> Public dicClassMemName As Dictionary(Of String, String)
    <XmlIgnoreAttribute(), _Weak()> Public ParsePrj As TSourceParser
    <XmlIgnoreAttribute(), _Weak()> Public theMain As TFunction
    <XmlIgnoreAttribute(), _Weak()> Public ArrayMaker As TFunction

    Public Sub New()
        Prj = CType(Me, TProject)
    End Sub

    Public Function IsSystemClass(cls As TClass) As Boolean
        Return ParsePrj.SystemClassNameList.Contains(cls.NameVar)
    End Function

    Public Sub OutputSourceFile()
        For Each lang In OutputLanguageList
            Debug.WriteLine("ソース 生成 {0} --------------------------------------------------------------------------------------------", lang)
            Select Case lang
                Case ELanguage.Basic
                    Dim basic_parser As New TBasicParser(Me)
                    MakeAllSourceCode(basic_parser)

                Case ELanguage.JavaScript, ELanguage.TypeScript
                    Dim script_parser As New TScriptParser(Me, lang)
                    MakeAllSourceCode(script_parser)

                Case Else
                    Debug.Assert(False)
            End Select
        Next

    End Sub

    Public Sub chk(b As Boolean)
        If Not b Then
            Debug.Assert(False)
        End If
    End Sub

    Public Function GetCla(name1 As String) As TClass
        Dim cla1 As TClass = Nothing

        If dicGenCla.ContainsKey(name1) Then
            cla1 = dicGenCla(name1)
            Return cla1
        End If

        If SimpleParameterizedClassTable.ContainsKey(name1) Then
            cla1 = SimpleParameterizedClassTable(name1)
            Return cla1
        Else
            Return Nothing
        End If
    End Function

    Public Function RegCla(name1 As String) As TClass
        Dim cla1 As TClass

        Debug.Assert(GetCla(name1) Is Nothing)

        cla1 = New TClass(Me, Nothing, name1)
        SimpleParameterizedClassList.Add(cla1)
        SimpleParameterizedClassTable.Add(cla1.NameCla(), cla1)

        Return cla1
    End Function

    Public Function GetDelegate(name1 As String) As TDelegate
        Dim cla1 As TDelegate

        cla1 = CType(GetCla(name1), TDelegate)
        Debug.Assert(cla1 IsNot Nothing)

        Return cla1
    End Function

    Public Function GetSpecializedClass(name1 As String, vtp As TList(Of TClass)) As TClass
        Dim cla1 As TClass, v As TList(Of TClass) = Nothing

        cla1 = GetCla(name1)
        Debug.Assert(cla1 IsNot Nothing AndAlso cla1.GenCla IsNot Nothing AndAlso cla1.GenCla.Count = vtp.Count AndAlso cla1.GenericType = EGeneric.ParameterizedClass)

        If dicCmpCla.ContainsKey(cla1) Then
            v = dicCmpCla(cla1)
            ' for Find
            For Each cla2 In v
                Debug.Assert(cla2.GenericType = EGeneric.SpecializedClass)

                ' 一致しない引数があるか調べる。
                Dim vidx = From idx In Sys.IndexList(cla2.GenCla) Where cla2.GenCla(idx) IsNot vtp(idx)
                If Not vidx.Any() Then
                    ' すべて一致する場合

                    Return cla2
                End If
            Next
        Else
            Debug.Print("")
        End If

        Return Nothing
    End Function

    ' 仮引数クラスを含む場合はtrue
    Public Function ContainsArgumentClass(cla1 As TClass) As Boolean
        Select Case cla1.GenericType
            Case EGeneric.SimpleClass
                Return False
            Case EGeneric.ParameterizedClass
                Return True
            Case EGeneric.ArgumentClass
                Return True
            Case EGeneric.SpecializedClass
                For Each cla2 In cla1.GenCla
                    If ContainsArgumentClass(cla2) Then
                        Return True
                    End If
                Next
                Return False
            Case Else
                Debug.Assert(False)
                Return False
        End Select
    End Function

    ' ジェネリック型のクラスを作る
    Public Function AddSpecializedClass(name1 As String, vtp As TList(Of TClass), dim_len As Integer) As TClass
        Dim cla1 As TClass, cla3 As TClass, v As TList(Of TClass) = Nothing

        cla1 = GetCla(name1)
        Debug.Assert(cla1 IsNot Nothing AndAlso cla1.GenCla IsNot Nothing AndAlso cla1.GenCla.Count = vtp.Count)

        Debug.Assert(dicCmpCla.ContainsKey(cla1))
        v = dicCmpCla(cla1)

        ' 新しくジェネリック型のクラスを作る
        If TypeOf cla1 Is TDelegate Then
            cla3 = New TDelegate(Me, cla1.NameCla())
        Else
            cla3 = New TClass(Me, cla1.NameCla())
        End If
        cla3.KndCla = cla1.KndCla
        cla3.GenericType = EGeneric.SpecializedClass
        cla3.OrgCla = cla1
        cla3.DimCla = dim_len

        cla3.GenCla = New TList(Of TClass)(From tp In vtp)
        For Each tp In vtp
            Debug.Assert(tp IsNot Nothing)
        Next

        v.Add(cla3)

        If cla3.GenCla(0).IsParamCla Then
            'Debug.Print("")
        Else

            If cla3.GenCla(0).NameCla() = "T" Then
                Debug.WriteLine("@@@@@@@@@@@@@@")
            End If
            For Each cla4 In SpecializedClassList
                Debug.Assert(cla4.LongName() <> cla3.LongName())
            Next
            SpecializedClassList.Add(cla3)
            If cla1.Parsed AndAlso Not ContainsArgumentClass(cla3) Then
                SetMemberOfSpecializedClass(cla3)
            Else
                PendingSpecializedClassList.Add(cla3)
            End If
        End If

        Return cla3
    End Function

    Public Function GetAddSpecializedClass(name1 As String, vtp As TList(Of TClass)) As TClass
        Dim cla1 As TClass

        cla1 = GetSpecializedClass(name1, vtp)
        If cla1 IsNot Nothing Then
            Return cla1
        End If

        cla1 = AddSpecializedClass(name1, vtp, 0)
        Return cla1
    End Function

    '  配列型を得る
    Public Function GetArrCla(cla1 As TClass, dim_cnt As Integer) As TClass
        Dim cla3 As TClass, v As TList(Of TClass) = Nothing

        Debug.Assert(dim_cnt <> 0)
        If dicArrCla.ContainsKey(cla1) Then
            v = dicArrCla(cla1)
            ' for Find
            For Each cla2 In v
                If cla2.DimCla = dim_cnt Then
                    ' 次元が同じ場合

                    Return cla2
                End If
            Next
        Else
            v = New TList(Of TClass)()
            dicArrCla.Add(cla1, v)
        End If

        '  新たに型を作る
        Dim vtp As New TList(Of TClass)
        vtp.Add(cla1)
        cla3 = AddSpecializedClass("Array", vtp, dim_cnt)
        v.Add(cla3)

        Return cla3
    End Function

    Public Function GetIEnumerableClass(cla1 As TClass) As TClass
        Dim vtp As New TList(Of TClass), cla2 As TClass

        vtp.Add(cla1)
        cla2 = GetAddSpecializedClass("IEnumerable", vtp)

        Return cla2
    End Function

    ' ジェネリック型を変換する
    Public Function SubstituteArgumentClass(tp As TClass, dic As Dictionary(Of String, TClass)) As TClass
        Dim name1 As String, vtp As TList(Of TClass), tp1 As TClass = Nothing, tp2 As TClass, changed As Boolean = False, cla1 As TClass

        If tp.GenericType = EGeneric.ArgumentClass AndAlso Not dic.ContainsKey(tp.NameCla()) Then
            Debug.Print("")
        End If

        If tp.DimCla <> 0 Then
            ' 配列型の場合

            ' 配列型を返す
            Debug.Assert(tp.NameCla() = "Array" AndAlso tp.GenCla IsNot Nothing AndAlso tp.GenCla.Count = 1)

            tp1 = tp.GenCla(0)
            tp2 = SubstituteArgumentClass(tp1, dic)
            If tp2 Is tp1 Then
                Return tp
            Else
                Return GetArrCla(tp2, tp.DimCla)
            End If
        End If

        If dic.ContainsKey(tp.NameCla()) Then
            If tp.GenericType <> EGeneric.ArgumentClass Then
                Debug.Print("")
            End If

            tp1 = dic(tp.NameCla())
            changed = True
            name1 = tp1.NameType()
        Else
            name1 = tp.NameType()
        End If

        If tp.GenCla Is Nothing Then
            ' ジェネリック型でない場合

            Debug.Assert(tp.DimCla = 0)
            If Not changed Then
                Return tp
            Else
                Return tp1
            End If
        Else
            ' ジェネリック型の場合

            vtp = New TList(Of TClass)()
            ' for Add Find
            For Each tp_f In tp.GenCla
                tp2 = SubstituteArgumentClass(tp_f, dic)
                If tp2 IsNot tp_f Then
                    changed = True
                End If
                vtp.Add(tp2)
            Next

            If changed Then
                ' 変換した場合

                ' ジェネリック型のクラスを得る
                cla1 = GetSpecializedClass(name1, vtp)
                If cla1 Is Nothing Then
                    ' ない場合

                    cla1 = AddSpecializedClass(name1, vtp, 0)
                    SetMemberOfSpecializedClass(cla1)
                End If

                Return cla1
            Else
                ' 変換しなかった場合

                Return tp
            End If
        End If
    End Function

    Public Function CopyVariable(var_src As TVariable, dic As Dictionary(Of String, TClass)) As TVariable
        Dim var1 As TVariable

        If TypeOf var_src Is TField Then
            var1 = New TField()
        ElseIf TypeOf var_src Is TLocalVariable Then
            var1 = New TLocalVariable()
        Else
            Debug.Assert(False)
            var1 = Nothing
        End If

        var_src.CopyVarMem(var1)
        var1.TypeVar = SubstituteArgumentClass(var_src.TypeVar, dic)

        Return var1
    End Function


    Public Function CopyField(cla1 As TClass, fld_src As TField, dic As Dictionary(Of String, TClass)) As TField
        Dim fld1 As New TField

        fld_src.CopyVarMem(fld1)

        fld1.ClaFld = cla1
        fld1.OrgFld = fld_src
        fld1.TypeVar = SubstituteArgumentClass(fld_src.TypeVar, dic)

        Return fld1
    End Function

    Public Function CopyFunctionDeclaration(cla1 As TClass, fnc_src As TFunction, dic As Dictionary(Of String, TClass)) As TFunction
        Dim fnc1 As New TFunction(fnc_src.NameVar, fnc_src.TypeVar)

        fnc_src.CopyFncMem(fnc1)

        fnc1.ClaFnc = cla1
        fnc1.OrgFnc = fnc_src
        fnc1.ArgFnc = New TList(Of TVariable)(From var_f In fnc_src.ArgFnc Select CopyVariable(var_f, dic))

        If fnc_src.RetType Is Nothing Then
            fnc1.RetType = Nothing
        Else
            fnc1.RetType = TProject.Prj.SubstituteArgumentClass(fnc_src.RetType, dic)
        End If

        Return fnc1
    End Function

    Public Sub SetMemberOfSpecializedClass(cla1 As TClass)
        Dim dic As Dictionary(Of String, TClass), i1 As Integer, dlg1 As TDelegate, dlg_org As TDelegate

        dic = New Dictionary(Of String, TClass)()
        ' for Each c In OrgCla.GenCla Add (c.NameCla, GenCla(@Idx)) To dic
        For i1 = 0 To cla1.OrgCla.GenCla.Count - 1
            dic.Add(cla1.OrgCla.GenCla(i1).NameCla(), cla1.GenCla(i1))
        Next

        cla1.DirectSuperClassList = New TList(Of TClass)(From spr1 In cla1.OrgCla.DirectSuperClassList Select TProject.Prj.SubstituteArgumentClass(spr1, dic))
        cla1.InterfaceList = New TList(Of TClass)(From spr1 In cla1.OrgCla.InterfaceList Select TProject.Prj.SubstituteArgumentClass(spr1, dic))
        cla1.FldCla = New TList(Of TField)(From fld1 In cla1.OrgCla.FldCla Select CopyField(cla1, fld1, dic))
        cla1.FncCla = New TList(Of TFunction)(From fnc1 In cla1.OrgCla.FncCla Select CopyFunctionDeclaration(cla1, fnc1, dic))

        If TypeOf cla1 Is TDelegate Then
            dlg1 = CType(cla1, TDelegate)
            dlg_org = CType(dlg1.OrgCla, TDelegate)

            dlg1.ArgDlg = New TList(Of TVariable)(From var_f In dlg_org.ArgDlg Select CopyVariable(var_f, dic))
            dlg1.RetDlg = TProject.Prj.SubstituteArgumentClass(dlg_org.RetDlg, dic)
        End If
    End Sub

    Public Function FindVariable(term As TTerm, name1 As String) As TVariable
        Dim cla1 As TClass = Nothing, var1 As TVariable

        Dim vfld = From fld In SystemType.FldCla Where fld.NameVar = name1
        If vfld.Any() Then

            Return vfld.First()
        End If

        For Each obj In Sys.AncestorList(term)
            If TypeOf obj Is TFrom Then
                With CType(obj, TFrom)
                    If .VarQry.NameVar = name1 Then
                        Return .VarQry
                    End If
                End With

            ElseIf TypeOf obj Is TAggregate Then
                With CType(obj, TAggregate)
                    If .VarQry.NameVar = name1 Then
                        Return .VarQry
                    End If
                End With

            ElseIf TypeOf obj Is TFor Then
                With CType(obj, TFor)
                    If .InVarFor IsNot Nothing AndAlso .InVarFor.NameVar = name1 Then
                        Return .InVarFor
                    End If
                    If .IdxVarFor IsNot Nothing AndAlso .IdxVarFor.NameVar = name1 Then
                        Return .IdxVarFor
                    End If
                End With

            ElseIf TypeOf obj Is TTry Then
                With CType(obj, TTry)
                    For Each var1 In .VarCatch
                        If var1.NameVar = name1 Then
                            Return var1
                        End If
                    Next
                End With

            ElseIf TypeOf obj Is TBlock Then
                With CType(obj, TBlock)
                    For Each var1 In .VarBlc
                        If var1.NameVar = name1 Then
                            Return var1
                        End If
                    Next

                End With

            ElseIf TypeOf obj Is TFunction Then
                With CType(obj, TFunction)
                    For Each var1 In .ArgFnc
                        If var1.NameVar = name1 Then
                            Return var1
                        End If
                    Next

                    If .ThisFnc.NameVar = name1 Then
                        Return .ThisFnc
                    End If
                End With
            End If
        Next

        If dicGenCla.ContainsKey(name1) Then
            cla1 = dicGenCla(name1)
            Return cla1
        End If

        If SimpleParameterizedClassTable.ContainsKey(name1) Then
            cla1 = SimpleParameterizedClassTable(name1)
            Return cla1
        End If

        Return Nothing
    End Function

    Public Function CanCnvCla(dst_cla As TClass, src_trm As TTerm, src_cla As TClass) As Boolean
        Dim dst_dlg As TDelegate, src_dlg As TDelegate, i1 As Integer

        If src_trm IsNot Nothing AndAlso TypeOf src_trm Is TReference AndAlso CType(src_trm, TReference).NameRef = "Nothing" Then
            Return Not dst_cla.IsSubcla(DoubleType)
        End If

        If TypeOf dst_cla Is TDelegate Then
            If Not TypeOf src_cla Is TDelegate Then
                Return False
            End If

            dst_dlg = CType(dst_cla, TDelegate)
            src_dlg = CType(src_cla, TDelegate)

            If Not CanCnvCla(dst_dlg.RetDlg, Nothing, src_dlg.RetDlg) Then
                Return False
            End If

            If dst_dlg.ArgDlg.Count <> src_dlg.ArgDlg.Count Then
                Return False
            End If

            For i1 = 0 To dst_dlg.ArgDlg.Count - 1
                If Not CanCnvCla(dst_dlg.ArgDlg(i1).TypeVar, Nothing, src_dlg.ArgDlg(i1).TypeVar) Then
                    Return False
                End If
            Next

            Return True
        End If

        If dst_cla Is StringType AndAlso src_cla Is CharType Then
            Return True
        End If

        If dst_cla.ContainsArgumentClass AndAlso dst_cla.OrgCla Is src_cla.OrgCla Then
            Return True
        End If

        Return dst_cla Is src_cla OrElse dst_cla Is ObjectType OrElse src_cla.IsSubcla(dst_cla)
    End Function

    Public Function MatchFncArg(fnc1 As TFunction, varg As TList(Of TTerm)) As Boolean
        Dim i1 As Integer, param_array As Boolean, var1 As TVariable, trm1 As TTerm, tp1 As TClass

        If varg Is Nothing Then
            Return True
        Else
            param_array = (fnc1.ArgFnc.Count <> 0 AndAlso fnc1.ArgFnc(fnc1.ArgFnc.Count - 1).ParamArrayVar)

            If fnc1.ArgFnc.Count = varg.Count OrElse param_array AndAlso fnc1.ArgFnc.Count - 1 <= varg.Count Then
                For i1 = 0 To fnc1.ArgFnc.Count - 1
                    If fnc1.ArgFnc(i1).ParamArrayVar Then
                        Return True
                    End If

                    var1 = fnc1.ArgFnc(i1)
                    trm1 = varg(i1)
                    tp1 = trm1.TypeTrm
                    If var1.TypeVar Is Nothing Then
                    ElseIf tp1 Is Nothing Then
                    ElseIf TypeOf trm1 Is TReference AndAlso CType(trm1, TReference).IsAddressOf Then
                    Else
                        If Not CanCnvCla(var1.TypeVar, trm1, tp1) Then
                            Return False
                        End If
                    End If
                Next
                Return True
            End If

            Return False
        End If
    End Function

    Public Function MatchFunction(fnc1 As TFunction, name1 As String, varg As TList(Of TTerm)) As Boolean
        Return fnc1.NameFnc() = name1 AndAlso Not fnc1.IsNew AndAlso Prj.MatchFncArg(fnc1, varg)
    End Function

    Public Shared Function FindFieldFunctionSub(cla1 As TClass, name1 As String, varg As TList(Of TTerm)) As TVariable
        Dim field_list = From fld2 In cla1.FldCla Where fld2.NameVar = name1
        If field_list.Any() Then
            Return field_list.First()
        End If

        Dim function_list = From fnc2 In cla1.FncCla Where Prj.MatchFunction(fnc2, name1, varg)
        If function_list.Any() Then
            Return function_list.First()
        End If

        Return Nothing
    End Function

    Public Shared Function FindFieldFunction(cla1 As TClass, name1 As String, varg As TList(Of TTerm)) As TVariable
        Dim variable_list = From var1 In (From cla2 In Concatenate(Prj.SystemType, cla1, Sys.ProperSuperClassList(cla1), Sys.AncestorInterfaceList(cla1)) Select FindFieldFunctionSub(CType(cla2, TClass), name1, varg)) Where var1 IsNot Nothing

        If variable_list.Any() _
            Then
            Return variable_list.First()
        Else
            Return Nothing
        End If
    End Function

    Public Shared Iterator Function Concatenate(ParamArray args As Object()) As IEnumerable(Of Object)
        For Each arg In args
            If TypeOf arg Is IEnumerable Then
                For Each o In CType(arg, IEnumerable)
                    Yield o
                Next
            Else
                Yield arg
            End If
        Next
    End Function

    Public Shared Function FindFieldByName(cla1 As TClass, name1 As String) As TField
        Dim fld1 As TField

        ' for Find
        For Each fld2 In cla1.FldCla
            If fld2.NameVar = name1 Then
                Return fld2
            End If
        Next

        ' for Find
        For Each cla_f In cla1.DirectSuperClassList
            fld1 = FindFieldByName(cla_f, name1)
            If fld1 IsNot Nothing Then
                Return fld1
            End If
        Next

        Return Nothing
    End Function

    Public Function FindFunctionByName(class_name As String, fnc_name As String) As TFunction
        For Each cls1 In SimpleParameterizedClassList
            For Each fnc1 In cls1.FncCla
                If cls1.NameCla() = class_name AndAlso fnc1.NameFnc() = fnc_name Then
                    Return fnc1
                End If
            Next
        Next

        Return Nothing
    End Function

    Public Function FindFieldByName(class_name As String, field_name As String) As TField
        For Each cls1 In SimpleParameterizedClassList
            For Each fld1 In cls1.FldCla
                If cls1.NameCla() = class_name AndAlso fld1.NameVar = field_name Then
                    Return fld1
                End If
            Next
        Next

        Return Nothing
    End Function

    Public Shared Function FindNew(cla1 As TClass, varg As TList(Of TTerm)) As TVariable
        For Each cla2 In Concatenate(cla1, Sys.ProperSuperClassList(cla1))
            Dim vfnc = From fnc In CType(cla2, TClass).FncCla Where fnc.IsNew AndAlso Prj.MatchFncArg(fnc, varg)
            If vfnc.Any() Then
                Return vfnc.First()
            End If
        Next

        Return Nothing
    End Function

    Public Sub DumpClass(cla1 As TClass, sw As TStringWriter)
        Dim i As Integer

        sw.Write("{0} {1} {2} ", cla1.ToString(), cla1.FldCla.Count, cla1.FncCla.Count)
        If cla1.GenCla IsNot Nothing Then
            sw.Write("<")
            For i = 0 To cla1.GenCla.Count - 1
                If i <> 0 Then
                    sw.Write(",")
                End If
                sw.Write(cla1.GenCla(i).ToString())
            Next
            sw.Write(">")
        End If
        sw.WriteLine("")

        If cla1.DirectSuperClassList.Count <> 0 Then
            sw.WriteLine("  super:{0}", cla1.DirectSuperClassList(0).ToString())
        End If
        If cla1.InterfaceList.Count <> 0 Then
            sw.Write("  impl:")
            For Each cla2 In cla1.InterfaceList
                sw.Write(" {0}", cla2.ToString())
            Next
            sw.WriteLine("")
        End If

    End Sub

    ' 指定されたクラスのクラスの初期化メソッドまたはインスタンスの初期化メソッドを作る。
    Public Sub MakeInstanceClassInitializerSub(cls1 As TClass, vnew As List(Of TFunction), is_shared As Boolean)
        Dim ini_fnc As TFunction, asn1 As TAssignment, call_ini As TCall, app_ini As TApply, dot1 As TDot, ref1 As TReference

        ' 初期化の式があるフィールドのリストを得る。
        Dim vfld = (From fld1 In cls1.FldCla Where fld1.InitVar IsNot Nothing AndAlso fld1.ModVar.isShared = is_shared).ToList()
        If vfld.Count <> 0 Then

            ' フィールドの初期化式から作った代入文を集めたメソッドを作る。
            If is_shared Then
                ' クラスの初期化の場合

                ini_fnc = New TFunction(TFunction.ClassInitializerName, Nothing)
            Else
                ' インスタンスの初期化の場合

                ini_fnc = New TFunction(TFunction.InstanceInitializerName, Nothing)
            End If
            cls1.FncCla.Add(ini_fnc)

            ini_fnc.ClaFnc = cls1
            ini_fnc.ModVar = New TModifier()
            ini_fnc.TypeFnc = EToken.Sub_
            ini_fnc.ThisFnc = New TLocalVariable(ParsePrj.ThisName, cls1)
            ini_fnc.BlcFnc = New TBlock()

            ' フィールドの初期化式から作った代入文をメソッドで定義する。
            For Each fld1 In vfld

                ref1 = New TReference(fld1)
                asn1 = New TAssignment(ref1, fld1.InitVar)
                ini_fnc.BlcFnc.AddStmtBlc(asn1)
            Next


            If is_shared Then
                ' クラスの初期化の場合

                ' クラスの初期化メソッドから、フィールドの初期化式から作った代入文を集めたメソッドを呼ぶ。
                dot1 = New TDot(New TReference(cls1), ini_fnc)
                app_ini = TApply.MakeAppCall(dot1)
                call_ini = New TCall(app_ini)
                call_ini.IsGenerated = True

                If theMain IsNot Nothing Then

                    theMain.BlcFnc.StmtBlc.Insert(0, call_ini)

                    theMain.CallTo.Add(ini_fnc)
                    ini_fnc.CallFrom.Add(theMain)
                End If
            Else
                ' インスタンスの初期化の場合

                ' すべてのコンストラクターに対し
                For Each new1 In vnew

                    ' インスタンスの初期化メソッドから、フィールドの初期化式から作った代入文を集めたメソッドを呼ぶ。
                    app_ini = TApply.MakeAppCall(New TReference(ini_fnc))
                    call_ini = New TCall(app_ini)
                    call_ini.IsGenerated = True
                    new1.BlcFnc.StmtBlc.Insert(0, call_ini)

                    new1.CallTo.Add(ini_fnc)
                    ini_fnc.CallFrom.Add(new1)
                Next
            End If
        End If
    End Sub

    ' すべての単純クラスとパラメータ化クラスに対し、クラスの初期化メソッドとインスタンスの初期化メソッドを作る。
    Public Sub MakeInstanceClassInitializer()
        ' すべての単純クラスとパラメータ化クラスに対し
        For Each cls1 In SimpleParameterizedClassList
            Dim vnew = (From fnc1 In cls1.FncCla Where fnc1.IsNew).ToList()

            If vnew.Count = 0 Then
                ' コンストラクターが１つもない場合

                ' 暗黙のコンストラクターを作る。
                Dim new_fnc As New TFunction(TFunction.ImplicitNewName, Nothing)

                new_fnc.ClaFnc = cls1
                new_fnc.ModVar = New TModifier()
                new_fnc.TypeFnc = EToken.New_
                new_fnc.ThisFnc = New TLocalVariable(ParsePrj.ThisName, cls1)
                new_fnc.BlcFnc = New TBlock()
                new_fnc.IsNew = True

                ' コンストラクターが最初になるようにする。
                cls1.FncCla.Insert(0, new_fnc)

                vnew.Add(new_fnc)
            End If

            ' クラスの初期化メソッドを作る。
            MakeInstanceClassInitializerSub(cls1, vnew, True)

            ' インスタンスの初期化メソッドを作る。
            MakeInstanceClassInitializerSub(cls1, vnew, False)
        Next
    End Sub

    Public Sub Let_(blc As TBlock, var1 As TVariable, trm As TTerm)
        Dim asn1 As New TAssignment(New TReference(var1), trm)

        blc.AddStmtBlc(asn1)
    End Sub


    ' 指定されたクラスのクラスの初期化メソッドまたはインスタンスの初期化メソッドを作る。
    Public Sub MakeXmlSerializer(cls1 As TClass)
        Dim ini_fnc As TFunction

        ' 初期化の式があるフィールドのリストを得る。
        Dim vfld = (From fld1 In cls1.FldCla Where fld1.isStrong() AndAlso Not fld1.ModVar.isShared).ToList()
        If vfld.Count <> 0 Then

            ' フィールドの初期化式から作った代入文を集めたメソッドを作る。
            ini_fnc = New TFunction(TFunction.InstanceInitializerName, Nothing)
            cls1.FncCla.Add(ini_fnc)

            ini_fnc.ClaFnc = cls1
            ini_fnc.ModVar = New TModifier()
            ini_fnc.TypeFnc = EToken.Sub_
            ini_fnc.ThisFnc = New TLocalVariable(ParsePrj.ThisName, cls1)
            ini_fnc.BlcFnc = New TBlock()

            ' フィールドの初期化式から作った代入文をメソッドで定義する。
            For Each fld1 In vfld

                Let_(ini_fnc.BlcFnc, fld1, fld1.InitVar)
            Next

        End If
    End Sub

    Public Function ElementType(type1 As TClass) As TClass
        chk(type1 IsNot Nothing)
        If type1.DimCla <> 0 OrElse type1.NameType() = "List" OrElse type1.NameType() = "TList" OrElse type1.NameType() = "IEnumerable" Then
            Debug.Assert(type1.GenCla IsNot Nothing AndAlso type1.GenCla.Count = 1)
            Return type1.GenCla(0)
        ElseIf type1.NameType() = "Dictionary" Then
            Debug.Assert(type1.GenCla IsNot Nothing AndAlso type1.GenCla.Count = 2)
            Return type1.GenCla(1)
        ElseIf type1.NameType() = "TMap" Then
            Debug.Assert(type1.GenCla IsNot Nothing AndAlso type1.GenCla.Count = 2)
            ' TMapの最初のフィールドdtの型の2番目のパラメータを持ってくる。
            Return type1.FldCla(0).TypeVar.GenCla(1)
        ElseIf type1.NameType().ToLower() = "string" Then
            Return CharType
        ElseIf type1.NameType() = "IList" Then
            Return ObjectType
        Else
            Debug.Assert(False)
            Return Nothing
        End If
    End Function

    ' フィールドがリストなどの場合は要素の型を返し、それ以外ならフィールドの型を返す。
    Public Function FieldElementType(fld As TField) As TClass
        Return CType(If(fld.TypeVar.OrgCla Is Nothing, fld.TypeVar, ElementType(fld.TypeVar)), TClass)
    End Function

    Public Function GetOperatorFunction(type1 As EToken, trm1 As TTerm) As TFunction
        Dim tp1 As TClass, name1 As String

        Select Case type1
            Case EToken.ADD
                name1 = "+"
            Case EToken.Mns
                name1 = "-"
            Case EToken.MUL
                name1 = "*"
            Case EToken.DIV
                name1 = "/"
            Case EToken.MOD_
                name1 = "Mod"
            Case EToken.Eq
                name1 = "="
            Case EToken.NE
                name1 = "<>"
            Case EToken.INC
                name1 = "++"
            Case EToken.DEC
                name1 = "--"
            Case EToken.BitOR
                name1 = "|"
            Case Else
                Return Nothing
        End Select

        ' 最初の引数の型を得る
        tp1 = trm1.TypeTrm
        Debug.Assert(tp1 IsNot Nothing)

        ' 名前が同じの演算子オーバーロード関数を探す
        Dim vfnc = From fnc In tp1.FncCla Where fnc.TypeFnc = EToken.Operator_ AndAlso fnc.NameFnc() = name1
        If vfnc.Any() Then
            Return vfnc.First()
        End If

        Return Nothing
    End Function

    '==========================================================================================================================================================================
    '==========================================================================================================================================================================
    Public Sub AddRuleCall(rule As TFunction, fnc1 As TFunction, cla1 As TClass)
        ' RuleのCall文を作る。
        Dim rule_fnc_list = From c In Sys.SuperClassList(cla1).Distinct() From f In c.FncCla Where f.OrgRule Is rule Select f
        If rule_fnc_list.Any() Then

            Dim rule_fnc As TFunction = rule_fnc_list.First()
            Dim rule_app As TApply = TApply.MakeAppCall(New TDot(Nothing, rule_fnc))

            Dim self_var As TVariable = fnc1.ArgFnc(0)
            Dim app_var As TVariable = fnc1.ArgFnc(1)

            rule_app.AddInArg(New TReference(self_var))
            rule_app.AddInArg(New TReference(app_var))

            fnc1.BlcFnc.AddStmtBlc(New TCall(rule_app))
        End If
    End Sub


    Public Function InitNavigateFunction(function_name As String, cla1 As TClass) As TFunction
        Dim fnc1 As New TFunction(function_name, Me, cla1)

        Debug.Print("Init Navigate Function {0}.{1}", cla1.NameVar, function_name)

        Dim self_var As New TLocalVariable("self", ObjectType)
        Dim app_var As New TLocalVariable("app", MainClass)
        fnc1.ArgFnc.Add(self_var)
        fnc1.ArgFnc.Add(app_var)
        fnc1.WithFnc = cla1

        Return fnc1
    End Function


    Public Function MakeStatementText(stmt As TStatement) As String
        Dim navi_make_source_code As New TNaviMakeSourceCode(Me, ParsePrj)
        navi_make_source_code.NaviStatement(stmt)

        Return TokenListToString(ParsePrj, stmt.TokenList)
    End Function

    Public Function MakeTermText(trm As TTerm) As String
        Dim navi_make_source_code As New TNaviMakeSourceCode(Me, ParsePrj)
        navi_make_source_code.NaviTerm(trm)

        Return TokenListToString(ParsePrj, trm.TokenList)
    End Function

    ' 仮想メソッド内の使用参照と定義参照の依存関係を求める。
    Public Sub VirtualizedMethodDefUseDependency(use_def As TUseDefineAnalysis)
        Dim dic As New Dictionary(Of TStatement, TApply)

        ' すべての仮想メソッドに対し
        For Each fnc1 In use_def.VirtualizedMethodList
            ' メソッド内の定義参照のリスト
            Dim def_list = From r In Sys.GetAllReference(fnc1.BlcFnc) Where r.DefRef

            ' メソッド内の定義参照に対し
            For Each ref1 In def_list

                ' ref1を含む文
                Dim ref1_up_stmt As TStatement = Sys.UpStmtProper(ref1)

                ' 余分な条件を取り除いた前提条件
                Dim cnd1 As TApply = Sys.GetCachedPreConditionClean(ref1_up_stmt, dic)

                ' CopyでUpTrmを使う。
                cnd1.UpTrm = ref1_up_stmt

                ' メソッド内でref11と同じ変数に対する局所変数か自身のフィールドの定義参照のリスト
                Dim use_list = From r In Sys.GetAllReference(fnc1.BlcFnc) Where Sys.OverlapRefPath(ref1, r) AndAlso Not r.DefRef AndAlso (Not TypeOf r Is TDot OrElse CType(r, TDot).IsSelfField())

                'Dim use_list2 = Enumerable.Distinct(use_list)
                For Each ref2 In use_list
                    ' ref2を含む文
                    Dim ref2_up_stmt As TStatement = Sys.UpStmtProper(ref2)

                    ' 余分な条件を取り除いた前提条件
                    Dim cnd2 As TApply = Sys.GetCachedPreConditionClean(ref2_up_stmt, dic)

                    If Sys.Consistent2(cnd1, cnd2) Then
                        ' 使用参照の文のAnd条件と定義参照の文のAnd条件が矛盾しない場合

                        'Debug.Print("ref1 up stmt {0} {1}", ref1.NameRef, MakeStatementText(ref1_up_stmt))
                        'Debug.Print("前提条件1 {0}", MakeTermText(cnd1))

                        'Debug.Print("ref2 up stmt {0}:{1} {2}", ref2.NameRef, ref2.IdxAtm, MakeStatementText(ref2_up_stmt))
                        'Debug.Print("前提条件2 {0}", MakeTermText(cnd2))
                        'Debug.Print("--------------------------------------------------------------------------------")
                    End If
                Next
            Next
        Next
    End Sub


    ' 親や子のフィールドの使用参照と定義参照の依存関係を求める。
    Public Sub VirtualizedMethodParentChildDefUseDependency(use_def As TUseDefineAnalysis)
        Dim dic As New Dictionary(Of TStatement, TApply)

        ' 仮想化メソッドのクラスのリスト
        Dim virtualized_all_class_list = From f In use_def.VirtualizedMethodList Select f.ClaFnc

        ' すべての仮想メソッドに対し
        For Each fnc1 In use_def.VirtualizedMethodList
            Debug.Print("メソッド {0} --------------------------------------------------------------------------------------------------------", fnc1.FullName())
            ' 仮想メソッド内のすべての参照のリストを得る。
            Dim parent_dot_list = From d In Sys.GetAllReference(fnc1.BlcFnc) Where TypeOf d Is TDot Select CType(d, TDot)

            Debug.Assert(parent_dot_list.Count() = Enumerable.Distinct(parent_dot_list).Count())

            ' 仮想メソッドが属するクラス内のすべてのフィールドに対し
            Dim super_class_strong_field_list = From f In Sys.SuperClassFieldList(fnc1.ClaFnc) Where f.ModVar.isStrong
            For Each fld1 In super_class_strong_field_list
                Debug.Print("フィールド {0} --------------------------------------------------------------------------------------------------------", fld1.FullFldName())
                ' フィールドの型
                Dim element_type As TClass = FieldElementType(fld1)

                ' フィールドの型/スーパークラスで仮想化クラスのリスト
                If (From c In virtualized_all_class_list Where Sys.SuperClassList(c).Contains(element_type)).Any() Then
                    ' フィールドの型/スーパークラスで仮想化クラスがある場合

                    ' フィールドの型の広義サブクラスで仮想化クラスのリスト
                    Dim virtualized_sub_class_list = Sys.SubClassList(element_type).Intersect(virtualized_all_class_list)

                    ' フィールドの型の広義サブクラスの仮想化クラスに対し
                    For Each cls1 In virtualized_sub_class_list
                        Debug.Print("クラス {0} --------------------------------------------------------------------------------------------------------", cls1.NameVar)
                        ' 子の仮想メソッド(fnc2 = fnc1の場合もありうる)
                        Dim fnc2 As TFunction = (From f In use_def.VirtualizedMethodList Where f.ClaFnc Is cls1).First()

                        ' 子の仮想メソッド内のすべての参照のリストを得る。
                        Dim child_dot_list = From d In Sys.GetAllReference(fnc2.BlcFnc) Where TypeOf d Is TDot Select CType(d, TDot)

                        '-------------------------------------------------- 親の仮想メソッドで定義した値を子の仮想メソッドで使用する場合
                        ' 子の仮想メソッド内で親のフィールドの使用参照のリストを求める。
                        Dim child_parent_dot_list = From d In child_dot_list Where d.IsParentField()

                        ' 親の仮想メソッド内のフィールドの定義参照に対し
                        For Each dot1 In From d In parent_dot_list Where d.TrmDot Is Nothing AndAlso d.DefRef

                            ' dot1を含む文の余分な条件を取り除いた前提条件
                            Dim dot1_up_stmt As TStatement = Sys.UpStmtProper(dot1)
                            Dim cnd1 As TApply = Sys.GetCachedPreConditionClean(dot1_up_stmt, dic)

                            ' CopyでUpTrmを使う。
                            cnd1.UpTrm = dot1_up_stmt

                            ' 元の仮想メソッドで参照パスが共通の定義参照のリストを得る。
                            Debug.Assert(child_parent_dot_list.Count() = Enumerable.Distinct(child_parent_dot_list).Count())
                            For Each dot2 In (From d In child_parent_dot_list Where Sys.OverlapRefPath(dot1, CType(d.UpTrm, TDot)))
                                Debug.Assert(Not dot2.DefRef)

                                ' dot2を含む文の余分な条件を取り除いた前提条件
                                Dim dot2_up_stmt As TStatement = Sys.UpStmtProper(dot2)
                                Dim cnd2 As TApply = Sys.GetCachedPreConditionClean(dot2_up_stmt, dic)

                                ' CopyでUpTrmを使う。
                                cnd2.UpTrm = dot2_up_stmt

                                ' 前提条件cnd2で親のフィールド参照を自身のフィールド参照に変換する。
                                Dim nrm_cnd As TApply = Sys.NormalizeReference(Me, cnd2, fld1)

                                If Sys.Consistent2(cnd1, nrm_cnd) Then
                                    ' 使用参照の文のAnd条件と定義参照の文のAnd条件が矛盾しない場合

                                    'Debug.Print("親Dot定義 {0}:{2} {1}", dot1.NameRef, MakeStatementText(dot1_up_stmt), dot1.IdxAtm)
                                    'Debug.Print("前提条件1 {0}", MakeTermText(cnd1))

                                    'Debug.Print("親Dot使用 {0}:{2} {1}", dot2.NameRef, MakeStatementText(dot2_up_stmt), dot2.IdxAtm)
                                    'Debug.Print("前提条件2 {0}", MakeTermText(cnd2))
                                    'Debug.Print("--------------------------------------------------------------------------------")
                                End If
                            Next
                        Next

                        '-------------------------------------------------- 子のメソッドで定義した値を親のメソッドで使用する場合
                        ' 子の仮想メソッド内の定義参照のリスト
                        Dim child_def_dot_list = From d In child_dot_list Where d.DefRef AndAlso d.TrmDot Is Nothing

                        ' 子の仮想メソッド内の定義参照に対し
                        For Each dot1 In child_def_dot_list

                            ' dot1を含む文
                            Dim dot1_up_stmt As TStatement = Sys.UpStmtProper(dot1)

                            ' 余分な条件を取り除いた前提条件
                            Dim cnd1 As TApply = Sys.GetCachedPreConditionClean(dot1_up_stmt, dic)

                            'If cnd1.ArgApp.Count <> 0 AndAlso cnd1.ArgApp(0) Is Nothing Then
                            '    cnd1 = Sys.GetPreConditionClean(dot1_up_stmt)
                            'End If

                            ' CopyでUpTrmを使う。
                            cnd1.UpTrm = dot1_up_stmt

                            ' 前提条件cnd1で親のフィールド参照を自身のフィールド参照に変換する。
                            Dim nrm_cnd As TApply = Sys.NormalizeReference(Me, cnd1, fld1)

                            ' 親の仮想メソッド内でfld1の使用参照のリスト
                            Dim parent_use_dot_list As List(Of TDot)

                            If fld1.TypeVar.OrgCla Is Nothing Then
                                ' フィールドがリストでない場合

                                ' 親の仮想メソッド内でfld1の使用参照のリストを求める。
                                parent_use_dot_list = (From d In parent_dot_list Where d.TrmDot Is Nothing AndAlso d.VarRef Is fld1 AndAlso TypeOf d.UpTrm Is TDot AndAlso Sys.OverlapRefPath(dot1, CType(d.UpTrm, TDot))).ToList()
                            Else
                                ' フィールドがリストの場合

                                ' 親の仮想メソッド内でfld1の使用参照のリストを求める。
                                parent_use_dot_list = (From d In parent_dot_list Where Sys.ChildElementDot(d, fld1) AndAlso Sys.OverlapRefPath(dot1, d)).ToList()
                            End If

                            ' 親の仮想メソッド内でfld1の使用参照に対し
                            For Each dot2 In parent_use_dot_list

                                ' dot2を含む文
                                Dim dot2_up_stmt As TStatement = Sys.UpStmtProper(dot2)

                                ' 余分な条件を取り除いた前提条件
                                Dim cnd2 As TApply = Sys.GetCachedPreConditionClean(dot2_up_stmt, dic)

                                If Sys.Consistent2(nrm_cnd, cnd2) Then
                                    ' 使用参照の文のAnd条件と定義参照の文のAnd条件が矛盾しない場合

                                    Debug.Print("子Dot定義 {0}:{2} {1}", dot1.NameRef, MakeStatementText(dot1_up_stmt), dot1.IdxAtm)
                                    Debug.Print("前提条件1 {0}", MakeTermText(cnd1))
                                    Debug.Print("前提条件 正規化 {0}", MakeTermText(nrm_cnd))

                                    Debug.Print("子Dot使用 {0}:{2} {1}", dot2.NameRef, MakeStatementText(dot2_up_stmt), dot2.IdxAtm)
                                    Debug.Print("前提条件2 {0}", MakeTermText(cnd2))
                                    Debug.Print("--------------------------------------------------------------------------------")
                                End If
                            Next
                        Next
                    Next
                End If
            Next
        Next
    End Sub


    ' 使用定義連鎖を伝播する。
    Public Sub PropagateUseDefineChain(use_def As TUseDefineAnalysis)
        ' 使用参照のリスト
        Dim use_ref_list = From r In use_def.UseDefineChainTable.Keys.ToList()

        ' すべての使用参照に対し
        For Each use_ref In use_ref_list
            ' この使用参照が依存する定義参照のリスト
            Dim use_def_chain As TUseDefine = use_def.UseDefineChainTable(use_ref)

            ' この使用参照が依存する定義参照に対し
            For Each child_chain In use_def_chain.DefineRefList

                ' 定義参照を含む代入文
                Dim stmt1 As TStatement = Sys.UpStmtProper(child_chain.UseRef)

                ' 代入文の実行条件
                Dim cnd1 As TApply

                ' 代入文の実行条件を得る。
                If use_def.StatementConditionTable.ContainsKey(stmt1) Then
                    cnd1 = use_def.StatementConditionTable(stmt1)
                Else
                    cnd1 = Sys.GetPreConditionClean(stmt1)
                    use_def.StatementConditionTable.Add(stmt1, cnd1)
                End If

                ' 代入文の実行条件と使用定義連鎖の実行条件のAndを得る。
                Dim and1 As TApply = TApply.NewOpr(EToken.And_)
                and1.ArgApp.Add(cnd1)
                and1.ArgApp.AddRange(From c In TUseDefine.ThisAncestorChainList(child_chain) Select CType(c.Cnd, TTerm))

                If Sys.Consistent(and1) Then
                    ' 矛盾しない場合


                End If
            Next
        Next
    End Sub


    Public Function RefPath(rule As TFunction) As TList(Of TClass)
        Dim result_class_list As New TList(Of TClass)

        ' 関数内のフィールド参照のリスト
        Dim dot_list = From d In rule.RefFnc Where TypeOf d.VarRef Is TField AndAlso TypeOf d Is TDot Select CType(d, TDot)

        ' 関数内のフィールド定義参照の最も内側のドットのリスト
        Dim def_inner_most_dot_list = From d In dot_list Where d.DefRef Select Sys.InnerMostDot(d)

        ' 関数内のフィールド使用参照の最も内側のドットに対し
        For Each dot1 In (From d In dot_list Where Not d.DefRef Select Sys.InnerMostDot(d))

            If dot1.TrmDot Is Nothing AndAlso dot1.VarRef.ModVar.isParent AndAlso TypeOf dot1.UpTrm Is TDot Then
                ' 親のフィールドを参照している場合

                Dim parent_dot As TDot = CType(dot1.UpTrm, TDot)

                ' パスが一致するフィールド定義参照の最も内側のドットのリスト
                Dim equal_def_inner_most_dot_list = From d In def_inner_most_dot_list Where d.TrmDot Is Nothing AndAlso Sys.OverlapRefPath(d, parent_dot)

                If equal_def_inner_most_dot_list.Any() Then
                    ' 代入されるフィールドに対し、親のフィールドとして参照している場合

                    ' 親の不変条件を適用してから、子の不変条件を適用する必要がある。

                    ' 親のフィールドをメンバーとして持つクラスのリスト
                    Dim parent_field_class_list = Sys.SubClassList(CType(parent_dot.VarRef, TField).ClaFld).Distinct()

                    ' 親のフィールドを参照するドットの処理対象クラスとそのスーパークラスのリスト
                    Dim super_class_list = Sys.SuperClassList(dot1.TypeDot).Distinct()

                    ' 上記のスーパークラスの型のフィールドのリスト
                    Dim parent_field_list = From parent_field In Prj.SimpleFieldList Where parent_field.isStrong() AndAlso super_class_list.Contains(FieldElementType(parent_field))

                    ' 親のフィールドを参照するドットの処理対象クラスの型のフィールドを持つクラスのリスト
                    Dim parent_field_class_list_2 = (From f In parent_field_list Select f.ClaFld).Distinct()

                    ' 親のフィールドをメンバーとして持ち、親のフィールドを参照するドットの処理対象クラスの型のフィールドを持つクラスの共通集合
                    Dim parent_field_class_list_3 = parent_field_class_list.Intersect(parent_field_class_list_2)

                    result_class_list.DistinctAddRange(parent_field_class_list_3)
                End If
            End If
        Next

        Return result_class_list
    End Function


    Public Function MakeSetParentSub(set_parent_name As String, cls1 As TClass) As TFunction
        Dim fnc1 As New TFunction(set_parent_name, Nothing)
        fnc1.ClaFnc = cls1
        fnc1.ModVar = New TModifier()
        fnc1.ModVar.isPublic = True
        fnc1.TypeFnc = EToken.Sub_
        fnc1.ThisFnc = New TLocalVariable(ParsePrj.ThisName, cls1)
        fnc1.BlcFnc = New TBlock()
        fnc1.IsNew = False
        fnc1.IsSetParent = True
        fnc1.WithFnc = cls1

        Dim self_var As New TLocalVariable("self", ObjectType)
        fnc1.ArgFnc.Add(self_var)

        Dim parent_var As New TLocalVariable("_Parent", ObjectType)
        fnc1.ArgFnc.Add(parent_var)

        Dim prev_var As New TLocalVariable("_Prev", ObjectType)
        fnc1.ArgFnc.Add(prev_var)

        Return fnc1
    End Function

    Public Function MakeNotNullIf(cnd As TTerm) As TIf
        Dim if1 As New TIf
        if1.IfBlc.Add(New TIfBlock(cnd, New TBlock()))

        Return if1
    End Function

    Public Function MakeNavigationFieldListTable(target_class_list As TList(Of TClass)) As TMap(Of TClass, TField)
        Dim reachable_from_bottom_field_pending As New TList(Of TField)

        For Each target_class In target_class_list

            ' 型がtarget_classかスーパークラスであるフィールドのリスト
            Dim parent_field_list = From parent_field In AppStrongFieldList Where FieldElementType(parent_field).IsSuperOrSubClassOf(target_class)

            ' reachable_from_bottom_field_pendingに入っていないparent_fieldを追加する。
            reachable_from_bottom_field_pending.DistinctAddRange(parent_field_list)
        Next

        Dim navigation_field_list_table As New TMap(Of TClass, TField)
        Dim reachable_from_bottom_processed As New List(Of TField)


        Dim parent_to_child_field_list_table As New TMap(Of TField, TField)

        Do While reachable_from_bottom_field_pending.Count <> 0
            Dim current_field = reachable_from_bottom_field_pending.Pop()
            reachable_from_bottom_processed.Add(current_field)

            ' 型がcurrent_fieldが属するクラスかスーパークラスであるフィールドのリスト
            Dim parent_field_list = From parent_field In AppStrongFieldList Where FieldElementType(parent_field).IsSuperOrSubClassOf(current_field.ClaFld)

            For Each parent_field In parent_field_list
                parent_to_child_field_list_table.Add(parent_field, current_field)
            Next

            ' 未処理のフィールド
            Dim not_processed_parent_field_list = From parent_field In parent_field_list Where Not reachable_from_bottom_processed.Contains(parent_field)

            reachable_from_bottom_field_pending.DistinctAddRange(not_processed_parent_field_list)
        Loop

        ' トップから到達可能のフィールドの未処理リスト
        Dim reachable_from_top_field_pending As New TList(Of TField)(From f In reachable_from_bottom_processed Where f.ClaFld.IsSuperClassOf(MainClass))

        ' トップから到達可能のフィールドの処理済みリスト
        Dim reachable_from_top_field_processed As New List(Of TField)

        Do While reachable_from_top_field_pending.Count <> 0
            ' reachable_from_top_field_pendingからcurrent_fieldを取り出し、reachable_from_top_field_processedに追加する。
            Dim current_field = reachable_from_top_field_pending.Pop()
            reachable_from_top_field_processed.Add(current_field)

            navigation_field_list_table.Add(current_field.ClaFld, current_field)

            If parent_to_child_field_list_table.ContainsKey(current_field) Then
                Dim child_field_list As List(Of TField) = parent_to_child_field_list_table(current_field)

                ' 未処理のフィールド
                Dim not_processed_chile_field_list = From chile_field In child_field_list Where Not reachable_from_top_field_processed.Contains(chile_field)

                reachable_from_top_field_pending.DistinctAddRange(not_processed_chile_field_list)
            End If
        Loop

        'Debug.Print("-----------------------------------")
        'For Each f In parent_to_child_field_list_table.Keys()
        '    For Each f2 In parent_to_child_field_list_table(f)
        '        Debug.Print("Navigation Field {0} {1}", f, f2)
        '    Next
        'Next

        Return navigation_field_list_table
    End Function

    Public Sub MakeNaviFunctionListSub(rule As TFunction, dt As TUseDefineAnalysis, function_name As String, dummy_function As TFunction, navi_needed_class_list As TList(Of TClass), cla1 As TClass, navigation_field_list As List(Of TField))
        Dim fnc1 As TFunction = InitNavigateFunction(function_name, cla1)
        dt.NaviFunctionList.Add(fnc1)

        If dt.UseParentClassList.Contains(cla1) Then
            ' 親のフィールドの値を参照している場合

            AddRuleCall(rule, fnc1, cla1)
        End If

        Dim self_var As TVariable = fnc1.ArgFnc(0)
        Dim app_var As TVariable = fnc1.ArgFnc(1)

        Debug.Print("Rule {0}", cla1.NameVar)

        For Each navigation_field In navigation_field_list
            If navigation_field.TypeVar.OrgCla IsNot Nothing Then
                ' リストの場合

                ' ループを作る。
                Dim for1 As New TFor
                for1.InVarFor = New TLocalVariable("x", Nothing)
                for1.InTrmFor = New TDot(Nothing, navigation_field)
                for1.BlcFor = New TBlock()

                ' リスト内の各要素に対しメソッドを呼ぶ。
                Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TReference(for1.InVarFor), dummy_function))
                app1.ArgApp.Add(New TReference(for1.InVarFor))
                app1.ArgApp.Add(New TReference(app_var))
                for1.BlcFor.AddStmtBlc(New TCall(app1))

                fnc1.BlcFnc.AddStmtBlc(for1)

                Debug.Print("For Each x in .{0}" + vbCrLf + "x.{1}()" + vbCrLf + "Next", navigation_field.NameVar, function_name)
            Else
                ' リストでない場合

                ' フィールドに対しメソッドを呼ぶ。
                Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TDot(Nothing, navigation_field), dummy_function))
                app1.ArgApp.Add(New TDot(Nothing, navigation_field))
                app1.ArgApp.Add(New TReference(app_var))
                fnc1.BlcFnc.AddStmtBlc(New TCall(app1))

                Debug.Print(".{0}()", function_name)
            End If

            navi_needed_class_list.DistinctAdd(FieldElementType(navigation_field))
        Next

        If Not dt.UseParentClassList.Contains(cla1) Then
            ' 親のフィールドの値を参照していない場合

            AddRuleCall(rule, fnc1, cla1)
        End If

    End Sub

    Public Sub MakeNaviFunctionList(rule As TFunction, dt As TUseDefineAnalysis, navigation_field_list_table As TMap(Of TClass, TField))

        Dim function_name As String = "Navigate_" + rule.NameVar

        Dim dummy_function As New TFunction(function_name, Nothing)
        Dim navi_needed_class_list As New TList(Of TClass)

        For Each cla1 In navigation_field_list_table.Keys()
            Dim navigation_field_list As List(Of TField) = navigation_field_list_table(cla1)

            MakeNaviFunctionListSub(rule, dt, function_name, dummy_function, navi_needed_class_list, cla1, navigation_field_list)
        Next

        navi_needed_class_list.DistinctAddRange(dt.VirtualizableClassList)
        For Each cla1 In navi_needed_class_list
            If Not navigation_field_list_table.Keys().Contains(cla1) Then
                Dim navigation_field_list As List(Of TField)

                Dim super_class_key_list = From c In navigation_field_list_table.Keys() Where c.IsSuperClassOf(cla1)
                If super_class_key_list.Any() Then
                    navigation_field_list = navigation_field_list_table(super_class_key_list.First())
                Else
                    navigation_field_list = New List(Of TField)()
                End If

                MakeNaviFunctionListSub(rule, dt, function_name, dummy_function, navi_needed_class_list, cla1, navigation_field_list)
            End If
        Next
    End Sub


    Public Function MakeSetParent() As List(Of TFunction)
        Dim function_list As New List(Of TFunction)

        If MainClass Is Nothing Then
            Return function_list
        End If

        Dim target_class_list As New TList(Of TClass)(From c In AppClassList Where (From f In c.FldCla Where f.ModVar.isParent OrElse f.ModVar.isPrev).Any())

        Dim navigation_field_list_table As TMap(Of TClass, TField) = MakeNavigationFieldListTable(target_class_list)

        Dim set_parent_name As String = "__SetParent"
        Dim dummy_function As New TFunction(set_parent_name, Nothing)

        Dim navi_needed_class_list As New TList(Of TClass)

        '        Dim target_navigation_class_list As New TList(Of TClass)(Sys.Union(Of TClass)(target_class_list, navigation_field_list_table.Keys()))
        Dim target_navigation_class_list As New TList(Of TClass)(target_class_list.Union(navigation_field_list_table.Keys()))

        For Each cls1 In target_navigation_class_list

            Dim fnc1 As TFunction = MakeSetParentSub(set_parent_name, cls1)
            Dim self_var As TVariable = fnc1.ArgFnc(0)
            Dim parent_var As TVariable = fnc1.ArgFnc(1)
            Dim prev_var As TVariable = fnc1.ArgFnc(2)

            function_list.Add(fnc1)

            ' 親フィールドを得る。
            For Each parent_field In (From f In cls1.FldCla Where f.ModVar.isParent)

                Dim if1 As TIf = MakeNotNullIf(TApply.NewTypeOf(New TReference(parent_var), parent_field.TypeVar))

                ' 親フィールドに親の値を代入する。
                if1.IfBlc(0).BlcIf.StmtBlc.Add(New TAssignment(New TDot(Nothing, parent_field), New TReference(parent_var)))

                fnc1.BlcFnc.AddStmtBlc(if1)
            Next

            ' 直前フィールドを得る。
            For Each prev_field In (From f In cls1.FldCla Where f.ModVar.isPrev)

                Dim if1 As TIf = MakeNotNullIf(TApply.NewTypeOf(New TReference(prev_var), prev_field.TypeVar))

                ' 直前フィールドに直前の値を代入する。
                if1.IfBlc(0).BlcIf.StmtBlc.Add(New TAssignment(New TDot(Nothing, prev_field), New TReference(prev_var)))

                fnc1.BlcFnc.AddStmtBlc(if1)
            Next

            If navigation_field_list_table.ContainsKey(cls1) Then

                Dim navigation_field_list As List(Of TField) = navigation_field_list_table(cls1)
                For Each navigation_field In navigation_field_list

                    Dim field_element_type As TClass = FieldElementType(navigation_field)
                    If field_element_type Is ObjectType Then

                        Debug.Print("ObjectType navigationfield : {0}", navigation_field)
                    End If

                    navi_needed_class_list.DistinctAdd(field_element_type)

                    If ApplicationClassList.Contains(navigation_field.TypeVar) Then
                        ' フィールドの型が単純クラスの場合

                        ' SetParentのCall文を作る。
                        Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TDot(Nothing, navigation_field), dummy_function))
                        app1.AddInArg(New TDot(Nothing, navigation_field))
                        app1.AddInArg(New TReference(self_var))
                        app1.ArgApp.Add(New TReference("Nothing"))

                        Dim if1 As TIf = MakeNotNullIf(TApply.NewOpr2(EToken.IsNot_, New TDot(Nothing, navigation_field), New TReference(ParsePrj.NullName())))
                        if1.IfBlc(0).BlcIf.StmtBlc.Add(New TCall(app1))

                        fnc1.BlcFnc.AddStmtBlc(if1)

                    ElseIf navigation_field.TypeVar.NameVar = "TList" Then
                        ' フィールドの型がリストの場合

                        Dim if1 As TIf = MakeNotNullIf(TApply.NewOpr2(EToken.IsNot_, New TDot(Nothing, navigation_field), New TReference(ParsePrj.NullName())))

                        ' リストの親フィールドに親の値を代入する。
                        Dim list_parent_field As TField = (From f In navigation_field.TypeVar.OrgCla.FldCla Where f.ModVar.isParent).First()
                        if1.IfBlc(0).BlcIf.AddStmtBlc(New TAssignment(New TDot(New TDot(Nothing, navigation_field), list_parent_field), New TReference(self_var)))

                        ' 直前のフィールドのリストを得る。
                        Dim element_prev_field_list = From f In Sys.SuperSubClassFieldList(field_element_type) Where f.ModVar.isPrev
                        Dim element_prev_var As TVariable = Nothing

                        If element_prev_field_list.Any() Then
                            ' 直前のフィールドがある場合

                            ' 直前の値の作業変数(__prev)を宣言する。
                            Dim var_decl As New TVariableDeclaration
                            element_prev_var = New TLocalVariable("__prev", field_element_type)
                            var_decl.VarDecl.Add(element_prev_var)
                            if1.IfBlc(0).BlcIf.AddStmtBlc(var_decl)
                        End If

                        Dim for1 As New TFor
                        for1.InVarFor = New TLocalVariable("x", field_element_type)
                        for1.InTrmFor = New TDot(Nothing, navigation_field)
                        for1.BlcFor = New TBlock()

                        Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TReference(for1.InVarFor), dummy_function))
                        app1.ArgApp.Add(New TReference(for1.InVarFor))

                        ' 親の引数に値を入れる。
                        If OutputLanguageList(0) = ELanguage.JavaScript Then

                            ' 親はself
                            app1.ArgApp.Add(New TReference(self_var))
                        Else

                            ' 親はリスト
                            app1.ArgApp.Add(New TDot(Nothing, navigation_field))
                        End If

                        If element_prev_field_list.Any() Then
                            ' 直前のフィールドがある場合

                            ' 直前の引数に値を入れる。
                            app1.ArgApp.Add(New TReference(element_prev_var))

                            ' SetParentを呼ぶ。
                            for1.BlcFor.AddStmtBlc(New TCall(app1))

                            ' 作業変数(__prev)を更新する。
                            for1.BlcFor.AddStmtBlc(New TAssignment(New TReference(element_prev_var), New TReference(for1.InVarFor)))
                        Else

                            ' 直前の引数に値を入れる。
                            app1.ArgApp.Add(New TReference("Nothing"))

                            ' SetParentを呼ぶ。
                            for1.BlcFor.AddStmtBlc(New TCall(app1))
                        End If

                        if1.IfBlc(0).BlcIf.AddStmtBlc(for1)

                        fnc1.BlcFnc.AddStmtBlc(if1)
                    Else
                    End If
                Next
            End If
        Next

        ' 実装済みのクラスのリスト
        Dim implemented_class_list As New TList(Of TClass)(target_navigation_class_list)

        For Each cls1 In navi_needed_class_list

            If Not (From c In implemented_class_list Where c.IsSuperClassOf(cls1)).Any Then
                ' 実装済みのスーパークラスがない場合

                function_list.Add(MakeSetParentSub(set_parent_name, cls1))

                implemented_class_list.Add(cls1)
            End If
        Next

        Dim i As Integer
        For i = function_list.Count - 1 To 0 Step -1
            Dim fnc1 As TFunction = function_list(i)
            If (From c In implemented_class_list Where c.IsProperSuperClassOf(fnc1.ClaFnc)).Any() Then
                ' 先祖のクラスで実装されている場合

                If fnc1.BlcFnc.StmtBlc.Count = 0 Then
                    ' 実行文がない場合

                    function_list.RemoveAt(i)
                Else
                    ' 実行文がある場合

                    fnc1.ModVar.isOverride = True

                    ' Baseを呼ぶ
                    Dim app1 As New TApply
                    app1.TypeApp = EToken.BaseCall
                    app1.FncApp = New TReference(set_parent_name)

                    app1.AddInArg(New TReference(fnc1.ArgFnc(0)))
                    app1.AddInArg(New TReference(fnc1.ArgFnc(1)))
                    app1.AddInArg(New TReference(fnc1.ArgFnc(2)))

                    fnc1.BlcFnc.StmtBlc.Insert(0, New TCall(app1))
                End If
            Else
                ' 先祖のクラスで実装されていない場合

                fnc1.ModVar.isVirtual = True
            End If
        Next

        For Each fnc1 In function_list
            fnc1.ClaFnc.FncCla.Add(fnc1)
        Next

        Return function_list
    End Function

    Public Sub EnsureFunctionIntegrity(fnc1 As TFunction)
        TExternal.SetParent(fnc1, fnc1.ClaFnc.FncCla)
        Dim set_parent_stmt As New TNaviSetParentStmt
        set_parent_stmt.NaviFunction(fnc1, Nothing)

        ' 関数内の参照をセットする
        Dim set_function As New TNaviSetFunction
        set_function.NaviFunction(fnc1, Nothing)

        Dim set_project_trm As New TNaviSetProjectTrm
        set_project_trm.NaviFunction(fnc1)

        ' 変数参照を解決する
        Dim set_ref As New TSetRefDeclarative
        set_ref.NaviFunction(fnc1)

        ' DefRefをセットする。
        Dim set_def_ref = New TNaviSetDefRef()
        set_def_ref.NaviFunction(fnc1, Nothing)

        Dim navi_set_ref_stmt As New TNaviSetRefStmt
        navi_set_ref_stmt.NaviFunction(fnc1)

        Dim set_up_trm As New TNaviSetUpTrm
        set_up_trm.NaviFunction(fnc1, Nothing)
    End Sub

    Public Sub Compile()
        Dim set_call As TNaviSetCall, nav_test As TNaviTest, set_parent_stmt As TNaviSetParentStmt, set_up_trm As TNaviSetUpTrm

        SrcPrj = New TList(Of TSourceFile)(From lib1 In LibraryList From fname In lib1.SourceFileNameList Select New TSourceFile(lib1, fname))

        Select Case Language
            Case ELanguage.Basic
                ParsePrj = New TBasicParser(Me)
            Case ELanguage.TypeScript, ELanguage.CSharp
                ParsePrj = New TScriptParser(Me, Language)
            Case Else
                Debug.Assert(False)
        End Select

        ' for ???
        For Each src_f In SrcPrj
            If src_f.FileSrc.StartsWith("@lib.") OrElse src_f.FileSrc.StartsWith("System.") OrElse src_f.FileSrc.StartsWith("sys.") OrElse src_f.FileSrc.StartsWith("web.") Then

                src_f.IsSystem = True
            End If
            Debug.Assert(src_f.vTextSrc Is Nothing)

            Dim src_file_path As String = Path.GetFullPath(ProjectHome + "\" + src_f.LibSrc.LibraryDirectory + "\" + src_f.FileSrc)

            If Language = ELanguage.Basic Then
                src_f.vTextSrc = TFile.ReadAllLines(src_file_path)
                src_f.LineTkn = New TList(Of TList(Of TToken))(From line1 In src_f.vTextSrc Select ParsePrj.Lex(line1))

            Else
                Dim src_text As String = TFile.ReadAllText(src_file_path)
                src_f.InputTokenList = ParsePrj.Lex(src_text)
            End If

            If Language <> ELanguage.CSharp Then
                ParsePrj.RegAllClass(Me, src_f)
            End If
        Next

        If Language = ELanguage.CSharp Then
            Exit Sub
        End If

        If Language = ELanguage.Basic Then
            For Each src_f In SrcPrj
                Debug.WriteLine("ソース:{0}", src_f.FileSrc)
                CurSrc = src_f
                ParsePrj.ClearParse()

                ParsePrj.ReadAllStatement(src_f)
                CurSrc = Nothing
            Next
        End If

        ' for Call
        For Each src_f In SrcPrj
            CurSrc = src_f
            ParsePrj.Parse(src_f)
            CurSrc = Nothing
        Next

        ' SetMemberOfSpecializedClassの中でPendingSpecializedClassListは変化せず、以降PendingSpecializedClassListを参照しない。
        For Each gen_cla In PendingSpecializedClassList
            SetMemberOfSpecializedClass(gen_cla)
        Next
        PendingSpecializedClassList = Nothing

        For Each cls1 In SimpleParameterizedClassList
            If cls1.NameCla() = MainClassName Then
                MainClass = cls1
            End If

            For Each fnc1 In cls1.FncCla
                If cls1.NameCla() = MainClassName AndAlso fnc1.NameFnc() = MainFunctionName Then
                    theMain = fnc1
                End If
                If cls1.NameCla() = "Array" AndAlso fnc1.NameFnc() = "CreateInstance" Then
                    ArrayMaker = fnc1
                End If
            Next
        Next
        If theMain Is Nothing Then
            Debug.Print("Mainがないですよ。")
        End If

        For Each cla1 In SimpleParameterizedClassList
            Debug.Assert(cla1.GenericType = EGeneric.SimpleClass OrElse cla1.GenericType = EGeneric.ParameterizedClass)
        Next

        Dim sw As New TStringWriter
        For Each cla1 In SimpleParameterizedClassList
            DumpClass(cla1, sw)
            cla1.SetAllSuperClass()
        Next


        sw.WriteLine("--------------------------------------------------------------------------------------------")
        For Each cla1 In SpecializedClassList
            DumpClass(cla1, sw)
            cla1.SetAllSuperClass()
        Next
        TFile.WriteAllText("C:\usr\prj\MyIDE\MyAlgo\a.txt", sw.ToString())

        ' すべての単純クラスとパラメータ化クラスに対し、クラスの初期化メソッドとインスタンスの初期化メソッドを作る。
        MakeInstanceClassInitializer()

        'set_parent_stmt = New TNaviSetParentStmt()
        'set_parent_stmt.NaviProject(Me, Nothing)
        TExternal.SetParent(Me, Nothing)

        Dim test_replace_term As New TNaviTestReplaceTerm
        test_replace_term.NaviProject(Me)

        ' 関数内の参照をセットする
        Dim set_function As New TNaviSetFunction
        set_function.NaviProject(Me, Nothing)

        Dim set_project_trm As New TNaviSetProjectTrm
        set_project_trm.NaviProject(Me)

        ' 変数参照を解決する
        Dim set_ref As New TSetRefDeclarative
        set_ref.NaviProject(Me)

        ' ForのLabelForをセットする。
        Dim navi_set_label = New TNaviSetLabel()
        navi_set_label.NaviProject(Me)

        ' DefRefをセットする。
        Dim set_def_ref = New TNaviSetDefRef()
        set_def_ref.NaviProject(Me, Nothing)

        Dim navi_set_ref_stmt As New TNaviSetRefStmt
        navi_set_ref_stmt.NaviProject(Me)

        Dim set_var_ref As New TNaviSetVarRef
        set_var_ref.NaviProject(Me, Nothing)
        Debug.Assert(set_ref.RefCnt = set_var_ref.RefCnt)

        set_call = New TNaviSetCall()
        set_call.NaviProject(Me, Nothing)

        Debug.Assert(set_ref.RefCnt = set_call.RefCnt)

        nav_test = New TNaviTest()
        nav_test.NaviProject(Me, Nothing)
        Debug.Assert(set_ref.RefCnt = nav_test.RefCnt)

        For Each cla1 In SimpleParameterizedClassList
            Debug.Assert(Not SpecializedClassList.Contains(cla1))
        Next
        For Each cla1 In SpecializedClassList
            Debug.Assert(Not SimpleParameterizedClassList.Contains(cla1) AndAlso cla1.GenericType = EGeneric.SpecializedClass)
        Next
        SimpleParameterizedSpecializedClassList = New TList(Of TClass)(SimpleParameterizedClassList)
        SimpleParameterizedSpecializedClassList.AddRange(SpecializedClassList)

        ApplicationClassList = New TList(Of TClass)(From c In SimpleParameterizedClassList Where c.GenericType = EGeneric.SimpleClass AndAlso c.SrcCla IsNot Nothing AndAlso Not c.SrcCla.IsSystem)

        ' サブクラスをセットする
        For Each cls1 In SimpleParameterizedSpecializedClassList
            For Each super_class In cls1.DirectSuperClassList
                super_class.DirectSubClassList.Add(cls1)
            Next
        Next

        ' 単純クラスのフィールドのリスト
        SimpleFieldList = (From cla1 In SimpleParameterizedClassList Where cla1.GenericType = EGeneric.SimpleClass From fld In cla1.FldCla Select fld).ToList()

        set_parent_stmt = New TNaviSetParentStmt()
        set_parent_stmt.NaviProject(Me, Nothing)

        set_up_trm = New TNaviSetUpTrm()
        set_up_trm.NaviProject(Me, Nothing)

        SetTokenListClsAll(ParsePrj)

        AppClassList = New TList(Of TClass)(From c In SimpleParameterizedClassList Where c.KndCla = EClass.ClassCla AndAlso c.GenericType = EGeneric.SimpleClass AndAlso Not IsSystemClass(c))
        AppFieldList = New TList(Of TField)(From c In AppClassList From f In c.FldCla Select f)
        AppStrongFieldList = New TList(Of TField)(From f In AppFieldList Where f.ModVar.isStrong())

        If MainClass IsNot Nothing Then

            ' すべての不変条件メソッドを呼ぶメソッド
            Dim all_rule As TFunction = InitNavigateFunction("AllRule", MainClass)

            Dim vrule = (From fnc In MainClass.FncCla Where fnc.ModVar.isInvariant).ToList()
            For Each rule In vrule
                Dim dt As New TUseDefineAnalysis

                ' 参照パスをセットする。
                Dim set_dependency As New TNaviSetDependency
                set_dependency.NaviFunction(rule)

                Dim use_parent_class_list As TList(Of TClass) = RefPath(rule)

                ' クラスの場合分けのIf文を探す。
                Dim set_virtualizable_if As New TNaviSetVirtualizableIf
                set_virtualizable_if.NaviFunction(rule)
                For Each c In set_virtualizable_if.VirtualizableClassList
                    Debug.Print("仮想化可能 {0}", c.NameVar)
                Next

                'Dim set_ref_path As New TNaviSetRefPath
                'set_ref_path.NaviFunction(rule)


                ' クラスの場合分けのIf文からクラスごとのメソッドを作る。
                Dim make_virtualizable_if_method As New TNaviMakeVirtualizableIfMethod
                make_virtualizable_if_method.OrgRuleNavi = rule
                make_virtualizable_if_method.UDA = dt
                make_virtualizable_if_method.NaviFunction(rule)

                ' 仮想メソッドの整合性を保つ。
                For Each fnc1 In dt.VirtualizedMethodList
                    EnsureFunctionIntegrity(fnc1)
                Next

                ' 仮想メソッド内の使用参照と定義参照の依存関係を求める。
                VirtualizedMethodDefUseDependency(dt)

                ' 親や子のフィールドの使用参照と定義参照の依存関係を求める。
                VirtualizedMethodParentChildDefUseDependency(dt)

                ' ナビゲート メソッドを作る。
                dt.UseParentClassList = use_parent_class_list
                dt.VirtualizableClassList = set_virtualizable_if.VirtualizableClassList

                Dim navigation_field_list_table As TMap(Of TClass, TField) = MakeNavigationFieldListTable(dt.VirtualizableClassList)

                MakeNaviFunctionList(rule, dt, navigation_field_list_table)

                ' ナビゲート メソッドの整合性を保つ。
                For Each fnc1 In dt.NaviFunctionList
                    EnsureFunctionIntegrity(fnc1)
                Next

                ' アプリのクラスの不変条件メソッドを得る。
                Dim app_rule = From fnc1 In dt.NaviFunctionList Where fnc1.ClaFnc.IsSuperClassOf(MainClass) AndAlso fnc1.ClaFnc IsNot ObjectType
                Debug.Assert(app_rule.Count() = 1)

                ' アプリのクラスの不変条件メソッドを呼ぶ。
                Dim app1 As TApply = TApply.MakeAppCall(New TDot(Nothing, app_rule.First()))
                app1.ArgApp.Add(New TReference(all_rule.ArgFnc(0)))
                app1.ArgApp.Add(New TReference(all_rule.ArgFnc(1)))
                all_rule.BlcFnc.AddStmtBlc(New TCall(app1))
            Next

            ' すべての不変条件メソッドの整合性を保つ。
            EnsureFunctionIntegrity(all_rule)
        End If

        Dim function_list As List(Of TFunction) = MakeSetParent()
        For Each fnc1 In function_list
            EnsureFunctionIntegrity(fnc1)
        Next

        ' オーバーロード関数をセットする
        SetOvrFnc()

        If theMain IsNot Nothing Then

            ' 間接呼び出しをセットする
            SetCallAll()
        End If
    End Sub

    Public Shared Function MakeProject(project_file_path As String) As TProject
        'XmlSerializerオブジェクトを作成
        Dim serializer As New XmlSerializer(GetType(TProject))

        '読み込むファイルを開く
        Dim sr As New StreamReader(project_file_path, Encoding.UTF8)

        'XMLファイルから読み込み、逆シリアル化する
        Dim prj1 As TProject = CType(serializer.Deserialize(sr), TProject)
        'ファイルを閉じる
        sr.Close()

        prj1.ProjectHome = Path.GetDirectoryName(project_file_path)

        For Each s In From x In prj1.LibraryList From y In x.SourceFileNameList Select y
            Debug.Print(s)
        Next

        If prj1.ClassNameTablePath <> "" Then
            prj1.ClassNameTable = TProgramTransformation.ReadClassNameTable(prj1.ClassNameTablePath, 2)
        End If

        prj1.Compile()

        Return prj1
    End Function

    '==========================================================================================================================================================================
    '==========================================================================================================================================================================

    ' オーバーロードしているメソッド(OvrFnc)とオーバーロードされているメソッド(OvredFnc)をセットする
    Public Sub SetOvrFncSub(cla1 As TClass, fnc1 As TFunction)
        Dim i1 As Integer, tp1 As TClass, tp2 As TClass, all_eq As Boolean

        ' すべてのスーパークラスに対し
        For Each cla2 In cla1.DirectSuperClassList

            ' すべてのメソッドに対し
            For Each fnc2 In cla2.FncCla
                If fnc2.NameFnc() = fnc1.NameFnc() AndAlso fnc2.ArgFnc.Count = fnc1.ArgFnc.Count Then
                    ' 名前と引数の数が同じ場合

                    all_eq = True
                    For i1 = 0 To fnc1.ArgFnc.Count - 1
                        tp1 = fnc1.ArgFnc(i1).TypeVar
                        tp2 = fnc2.ArgFnc(i1).TypeVar
                        If tp1 IsNot tp2 Then
                            ' 引数の型が違う場合

                            Debug.WriteLine("引数の型が違う {0} {1} {2}", cla1.NameCla(), fnc1.NameFnc(), cla2.NameCla())
                            all_eq = False
                            Exit For
                        End If
                    Next

                    If all_eq Then
                        ' すべての引数の型が同じ場合

                        ' fnc1のオーバーロード関数を設定する
                        fnc1.OvrFnc = fnc2
                        fnc2.OvredFnc.Add(fnc1)
                        Exit Sub
                    End If
                End If
            Next

            If fnc1.OvrFnc Is Nothing Then
                ' cla2のメソッドにfnc1のオーバーロード関数がない場合

                ' cla2のスーパークラスの中でfnc1のオーバーロード関数を探す
                SetOvrFncSub(cla2, fnc1)
                If fnc1.OvrFnc IsNot Nothing Then
                    ' cla2のスーパークラスの中にfnc1のオーバーロード関数があった場合
                    Exit Sub
                End If
            End If
        Next
    End Sub

    ' fnc1をオーバーロードしているメソッド(OvredFnc)の子孫をセットする
    Public Sub SetEqOvredFncAll(fnc1 As TFunction, vfnc As TList(Of TFunction))
        For Each fnc2 In fnc1.OvredFnc
            vfnc.Add(fnc2)

            ' fnc2のオーバーロード関数の子孫をセットする
            SetEqOvredFncAll(fnc2, vfnc)
        Next
    End Sub

    ' オーバーロード関数をセットする
    Public Sub SetOvrFnc()
        '  すべてのクラスに対し
        For Each cla1 In SimpleParameterizedClassList
            For Each fnc1 In cla1.FncCla
                If Not vAllFnc.Contains(fnc1) Then
                    vAllFnc.Add(fnc1)
                End If

                For Each fnc2 In fnc1.CallTo
                    If Not vAllFnc.Contains(fnc2) Then
                        vAllFnc.Add(fnc2)
                    End If
                Next
            Next
        Next

        For Each cla1 In SimpleParameterizedClassList
            For Each fld1 In cla1.FldCla
                If vAllFld.Contains(fld1) Then
                    Debug.Assert(False)
                Else
                    vAllFld.Add(fld1)
                End If
            Next
        Next
        For Each cla1 In SpecializedClassList
            For Each fld1 In cla1.FldCla
                If vAllFld.Contains(fld1) Then
                    Debug.Assert(False)
                Else
                    vAllFld.Add(fld1)
                End If
            Next
        Next

        '  すべてのクラスに対し
        For Each fnc1 In vAllFnc
            If fnc1.ModFnc().isOverride Then

                ' オーバーロードしているメソッド(OvrFnc)とオーバーロードされているメソッド(OvredFnc)をセットする
                SetOvrFncSub(fnc1.ClaFnc, fnc1)
                Debug.Assert(fnc1.OvrFnc IsNot Nothing)
            End If
        Next

        '  すべてのクラスに対し
        For Each fnc1 In vAllFnc
            Debug.Assert(fnc1.EqOvredFncAll.Count = 0)

            ' fnc1自身をEqOvredFncAllに追加する
            fnc1.EqOvredFncAll.Add(fnc1)

            ' fnc1をオーバーロードをしているメソッドの子孫をEqOvredFncAllに追加する
            SetEqOvredFncAll(fnc1, fnc1.EqOvredFncAll)
        Next
    End Sub

    ' 間接呼び出しをセットする
    Public Sub SetCallAll()
        Dim changed As Boolean, v As New TList(Of TFunction), i1 As Integer

        '  すべてのクラスに対し
        For Each cla1 In SimpleParameterizedClassList

            '  すべてのメソッドに対し
            For Each fnc1 In cla1.FncCla

                Debug.Assert(fnc1.CallToAll.Count = 0)

                ' fnc1から呼んでいるすべてのメソッドに対し
                For Each fnc2 In fnc1.CallTo

                    ' fnc2とfnc2をオーバーロードをしているメソッドの子孫(EqOvredFncAll)をCallToAllにセットする
                    fnc1.CallToAll.AddRange(fnc2.EqOvredFncAll)
                Next

                For Each fnc2 In fnc1.CallToAll
                    'Debug.WriteLine("call to All {0} - {1}", fnc1.FullName(), fnc2.FullName())
                Next
            Next
        Next

        i1 = 1
        Do While True
            changed = False

            '  すべてのクラスに対し
            For Each cla1 In SimpleParameterizedClassList

                '  すべてのメソッドに対し
                For Each fnc1 In cla1.FncCla

                    v.Clear()

                    ' fnc1から直接/間接に呼んでいるすべてのメソッドに対し
                    For Each fnc2 In fnc1.CallToAll

                        ' fnc2から直接/間接に呼んでいるすべてのメソッドに対し
                        For Each fnc3 In fnc2.CallToAll

                            If Not fnc1.CallToAll.Contains(fnc3) AndAlso Not v.Contains(fnc3) Then
                                v.Add(fnc3)
                            End If
                        Next
                    Next

                    If v.Count <> 0 Then
                        fnc1.CallToAll.AddRange(v)
                        changed = True
                    End If
                Next
            Next

            Debug.WriteLine("間接呼び出し {0}", i1.ToString())
            i1 = i1 + 1

            If Not changed Then
                Exit Do
            End If
        Loop

        '  すべてのクラスのすべてのメソッドに対し、Reachableをセットする
        For Each fnc1 In vAllFnc
            fnc1.Reachable = fnc1 Is theMain OrElse theMain.CallToAll.Contains(fnc1)
            If fnc1.OrgFnc IsNot Nothing Then
                fnc1.OrgFnc.Reachable = (fnc1.OrgFnc.Reachable OrElse fnc1.Reachable)
                Debug.Assert(fnc1.OrgFnc.OrgFnc Is Nothing)
            End If

            Debug.Assert(fnc1.ClaFnc IsNot Nothing)
            If fnc1.Reachable Then
                fnc1.ClaFnc.UsedVar = True
                If fnc1.OrgFnc IsNot Nothing Then
                    fnc1.OrgFnc.ClaFnc.UsedVar = True
                End If
            End If
        Next

        '  すべてのフィールドに対し
        For Each fld1 In vAllFld
            For Each ref1 In fld1.RefVar
                If ref1.FunctionTrm.Reachable Then
                    fld1.UsedVar = True
                    If fld1.OrgFld IsNot Nothing Then
                        fld1.OrgFld.UsedVar = (fld1.OrgFld.UsedVar OrElse fld1.UsedVar)
                        Debug.Assert(fld1.OrgFld.OrgFld Is Nothing)
                    End If

                    Exit For
                End If
            Next
            Debug.Assert(fld1.ClaFld IsNot Nothing)
            If fld1.UsedVar Then
                fld1.ClaFld.UsedVar = True
                If fld1.OrgFld IsNot Nothing Then
                    fld1.OrgFld.ClaFld.UsedVar = True
                End If
            End If
        Next

        ' すべてのクラスに対し
        For Each cls1 In SimpleParameterizedSpecializedClassList
            If cls1.UsedVar Then
                For Each cls2 In cls1.ProperSuperClassListOLD
                    cls2.UsedVar = True
                Next
            End If
        Next
    End Sub

    ' 関数のノードをグラフに追加する
    Public Function AddFncGraph(dic1 As Dictionary(Of Object, TFlowNode), fnc1 As TFunction) As TFncNode
        Dim fncnd As TFncNode, fncnd2 As TFncNode, fnc2 As TFunction

        ' 関数のノードを作る。
        fncnd = New TFncNode(fnc1)

        ' 辞書に追加する。
        dic1.Add(fnc1, fncnd)

        ' fnc1がどこから呼ばれ得るかを調べる。
        ' fnc1とそのすべてのスーパークラスの関数についてループする。
        fnc2 = fnc1
        Do While fnc2 IsNot Nothing

            If fnc2 IsNot fnc1 AndAlso dic1.ContainsKey(fnc2) Then
                ' 辞書に登録済みの場合

                'Debug.WriteLine("処理済み {0}", fnc2.FullName())
            Else
                ' 辞書に登録済みでない場合

                ' すべての関数呼び出しに対し
                For Each ref2 In fnc2.RefVar

                    If ref2.FunctionTrm.Reachable Then
                        ' 到達可能の関数内で参照されている場合

                        If dic1.ContainsKey(ref2.FunctionTrm) Then
                            ' 関数が辞書にある場合

                            fncnd2 = dic1(ref2.FunctionTrm)
                        Else
                            ' 関数が辞書にない場合

                            ' 関数のノードをグラフに追加する
                            fncnd2 = AddFncGraph(dic1, ref2.FunctionTrm)
                        End If

                        ' 関数から関数への矢印を追加する
                        If Not fncnd2.ToNd.ContainsKey(fnc1) Then
                            fncnd2.ToNd.Add(fnc1, fncnd)
                        End If
                    End If
                Next
            End If

            ' コード上はfnc1のスーパークラスの関数fnc2の呼び出しでも、実際はfnc1が呼ばれる場合がある
            fnc2 = fnc2.OvrFnc
        Loop

        Return fncnd
    End Function

    ' 変数参照のノードをグラフに追加する
    Public Function AddRefGraph(dic1 As Dictionary(Of Object, TFlowNode), ref1 As TReference) As TRefNode
        Dim refnd As TRefNode, fncnd As TFncNode

        ' 変数参照のノードを作る。
        refnd = New TRefNode(ref1)

        ' 辞書に追加する。
        Debug.Assert(Not dic1.ContainsKey(ref1))
        dic1.Add(ref1, refnd)

        If dic1.ContainsKey(ref1.FunctionTrm) Then
            ' 変数参照を含む関数のノードが辞書にある場合

            fncnd = CType(dic1(ref1.FunctionTrm), TFncNode)
        Else
            ' 変数参照を含む関数のノードが辞書にない場合

            ' 関数のノードをグラフに追加する
            fncnd = AddFncGraph(dic1, ref1.FunctionTrm)
        End If

        ' 関数から変数参照への矢印を追加する。
        If Not fncnd.ToNd.ContainsKey(ref1) Then

            fncnd.ToNd.Add(ref1, refnd)
        End If

        Return refnd
    End Function

    ' 変数参照のグラフを作る
    Public Sub MakeReferenceGraph()
        Dim dic1 As New Dictionary(Of Object, TFlowNode), vnd As TList(Of TNode), vfnc As List(Of TFunction)
        Dim dgr As TDrawGraph, dot_dir As String, dot_path As String, idx As Integer, def_ref As Boolean
        Dim sw As New TStringWriter, file_name As String

        dot_dir = OutputDirectory + "\html\_dot"

        ' すべてのクラスに対し
        For Each cla1 In SimpleParameterizedClassList

            If SimpleParameterizedClassList.IndexOf(cla1) Mod 25 = 0 Then
                Debug.WriteLine("Make Ref Graph {0}", SimpleParameterizedClassList.IndexOf(cla1))
            End If

            ' すべてのフィールドに対し
            For Each fld1 In cla1.FldCla

                If True OrElse cla1.NameVar = "TApply" AndAlso fld1.NameVar = "KndApp" Then

                    For idx = 0 To 1

                        def_ref = If(idx = 0, True, False)

                        ' ノードの辞書を初期化する
                        TFlowNode.CntNd = 0
                        dic1 = New Dictionary(Of Object, TFlowNode)()
                        vfnc = New List(Of TFunction)()

                        ' すべてのフィールド参照に対し
                        For Each ref1 In fld1.RefVar

                            If ref1.DefRef = def_ref AndAlso ref1.FunctionTrm.Reachable Then
                                ' 到達可能の関数内で参照されている場合

                                If Not vfnc.Contains(ref1.FunctionTrm) Then
                                    ' 同一関数内での変数参照が未処理の場合

                                    vfnc.Add(ref1.FunctionTrm)

                                    ' 変数参照のノードをグラフに追加する
                                    AddRefGraph(dic1, ref1)
                                End If
                            End If
                        Next

                        ' ノードの集合からグラフを作る
                        vnd = TGraph.Node2Graph(New TList(Of TFlowNode)(dic1.Values))

                        dgr = New TDrawGraph(vnd)
                        TDrawGraph.CheckGraph(dgr.AllNode)

                        TDirectory.CreateDirectory(dot_dir)

                        file_name = GetHtmlFileName(cla1) + "_" + GetHtmlFileName(fld1) + "_" + If(def_ref, "define", "use")
                        dot_path = dot_dir + "\" + file_name + ".dot"

                        ' dotファイルに書く
                        TGraph.WriteDotFile("オリジナル", dgr.AllNode, Nothing, dot_path)

                        sw.WriteLine("dot -Tsvg {0}.dot -o {0}.svg", file_name)
                    Next
                End If
            Next
        Next

        TFile.WriteAllText(dot_dir + "\DotToSvg.bat", sw.ToString(), Encoding.GetEncoding("Shift_JIS"))
    End Sub

    Public Sub SetDicMemNameJava()
        Dim dic1 As Dictionary(Of String, String)

        dicMemName = New Dictionary(Of String, Dictionary(Of String, String))()

        dic1 = New Dictionary(Of String, String)()
        dic1.Add("Length", "length")
        dic1.Add("Clone", "clone")
        dicMemName.Add("Array", dic1)

        dic1 = New Dictionary(Of String, String)()
        dic1.Add("Add", "add")
        dic1.Add("AddRange", "addAll")
        dic1.Add("Contains", "contains")
        dic1.Add("Count", "size()")
        dic1.Add("IndexOf", "indexOf")
        dic1.Add("Insert", "add")
        dic1.Add("Remove", "remove")
        dic1.Add("RemoveAt", "remove")
        dic1.Add("Clear", "clear")
        dicMemName.Add("List", dic1)

        dic1 = New Dictionary(Of String, String)()
        dic1.Add("Count", "size()")
        dic1.Add("Peek", "peek")
        dic1.Add("Pop", "pop")
        dic1.Add("Push", "push")
        dicMemName.Add("Stack", dic1)

        dic1 = New Dictionary(Of String, String)()
        dic1.Add("Clear", "clear")
        dic1.Add("ContainsKey", "containsKey")
        dic1.Add("Count", "length()")
        dic1.Add("Length", "length()")
        dic1.Add("IndexOf", "indexOf")
        dic1.Add("Replace", "replace")
        dic1.Add("Substring", "substring")
        dic1.Add("ToLower", "toLowerCase")
        dicMemName.Add("String", dic1)

        dic1 = New Dictionary(Of String, String)()
        dic1.Add("ContainsKey", "containsKey")
        dic1.Add("Add", "put")
        dic1.Add("Keys", "keySet()")
        dic1.Add("Clear", "clear")
        dic1.Add("Remove", "remove")
        dic1.Add("Values", "values()")
        dicMemName.Add("Dictionary", dic1)

        dic1 = New Dictionary(Of String, String)()
        dic1.Add("Abs", "abs")
        dic1.Add("Ceiling", "ceil")
        dic1.Add("Floor", "floor")
        dic1.Add("Max", "max")
        dic1.Add("Min", "min")
        dic1.Add("Round", "round")
        dic1.Add("Sqrt", "sqrt")
        dicMemName.Add("Math", dic1)

        dicClassMemName = New Dictionary(Of String, String)()
        dicClassMemName.Add("Double.NaN", "Double.NaN")
        dicClassMemName.Add("Double.IsNaN", "Double.isNaN")
        dicClassMemName.Add("Double.MaxValue", "Double.MAX_VALUE")
        dicClassMemName.Add("Double.MinValue", "Double.MIN_VALUE")
        dicClassMemName.Add("Char.IsDigit", "Character.isDigit")
        dicClassMemName.Add("Char.IsWhiteSpace", "Character.isWhitespace")
        dicClassMemName.Add("Char.IsLetter", "Character.isLetter")
        dicClassMemName.Add("Char.IsLetterOrDigit", "Character.isLetterOrDigit")
        dicClassMemName.Add("Integer.Parse", "Integer.parseInt")
        '        dicClassMemName.Add("", "")
    End Sub

    Public Function TypeName(name1 As String) As String
        If name1 = "int" Then
            Return "Integer"
        ElseIf name1 = "bool" Then
            Return "Boolean"
        End If

        If ClassNameTable IsNot Nothing AndAlso ClassNameTable.ContainsKey(name1) Then
            Return ClassNameTable(name1)
        End If

        Return name1
    End Function

    Public Sub SetClassNameList(cla1 As TClass, parser As TSourceParser)
        Dim tw As New TTokenWriter(cla1, parser)
        Dim i1 As Integer

        If cla1.TokenListVar IsNot Nothing Then
            Exit Sub
        End If

        If cla1 Is Nothing Then
            tw.Fmt("型不明")
        Else

            If cla1.DimCla <> 0 Then
                ' 配列の場合

                Debug.Assert(cla1.GenCla IsNot Nothing AndAlso cla1.GenCla.Count = 1)
                SetClassNameList(cla1.GenCla(0), parser)
                tw.Fmt(cla1.GenCla(0).TokenListVar, EToken.LP)

                For i1 = 0 To cla1.DimCla - 1
                    If i1 <> 0 Then
                        tw.Fmt(EToken.Comma)
                    End If
                Next
                tw.Fmt(EToken.RP)
            Else
                ' 配列でない場合
                tw.Fmt(cla1.NameVar)
                If cla1.GenCla IsNot Nothing Then
                    ' 総称型の場合

                    tw.Fmt(EToken.LP, EToken.Of_)
                    For i1 = 0 To cla1.GenCla.Count - 1
                        If i1 <> 0 Then
                            tw.Fmt(EToken.Comma)
                        End If

                        SetClassNameList(cla1.GenCla(i1), parser)
                        tw.Fmt(cla1.GenCla(i1).TokenListVar)
                    Next
                    tw.Fmt(EToken.RP)
                End If
            End If
        End If

        cla1.TokenListVar = tw.GetTokenList()

    End Sub

    Public Sub SetTokenListClsAll(parser As TSourceParser)
        For Each cla1 In SimpleParameterizedSpecializedClassList
            SetClassNameList(cla1, parser)
        Next
    End Sub

    Public Function TokenListToString(parser As TSourceParser, v As List(Of TToken)) As String
        Dim sw As New TStringWriter, start_of_line As Boolean = True

        For Each tkn In v
            Dim txt As String = ""

            Select Case tkn.TypeTkn
                Case EToken.Int
                Case EToken.NL
                Case EToken.Unknown
                Case EToken.Comment
                Case EToken.Tab
                Case Else
                    If parser.vTknName.ContainsKey(tkn.TypeTkn) Then
                        txt = parser.vTknName(tkn.TypeTkn)
                    Else
                        txt = "未登録語 : " + tkn.TypeTkn.ToString()
                        parser.vTknName.Add(tkn.TypeTkn, txt)
                        Debug.Print(txt)
                        '                        Debug.Assert(False)
                    End If

            End Select

            Select Case tkn.TypeTkn
                Case EToken.Int
                    sw.Write(tkn.StrTkn)

                Case EToken.NL
                    sw.WriteLine("")

                Case EToken.Tab
                    Dim k As Integer
                    For k = 0 To tkn.TabTkn * 4 - 1
                        sw.Write(" ")
                    Next

                Case EToken.Comment
                    If parser.LanguageSP = ELanguage.Basic Then
                        sw.Write("'" + tkn.StrTkn)
                    Else
                        sw.Write("//" + tkn.StrTkn)
                    End If

                Case EToken.As_, EToken.To_, EToken.Is_, EToken.IsNot_, EToken.In_, EToken.Into_, EToken.Where_, EToken.Take_, EToken.Step_, EToken.Implements_, EToken.ParamArray_
                    sw.Write(" " + txt + " ")

                Case EToken.Then_
                    sw.Write(" " + txt)

                Case EToken.Char_, EToken.String_
                    sw.Write(tkn.StrTkn)

                Case EToken.Unknown
                    If TypeOf tkn.ObjTkn Is TDot Then
                        With CType(tkn.ObjTkn, TDot)
                            sw.Write(".{0}", parser.TranslageReferenceName(CType(tkn.ObjTkn, TDot)))

                        End With

                    ElseIf TypeOf tkn.ObjTkn Is TReference Then
                        With CType(tkn.ObjTkn, TReference)
                            sw.Write(parser.TranslageReferenceName(CType(tkn.ObjTkn, TReference)))
                        End With

                    ElseIf TypeOf tkn.ObjTkn Is TClass Then
                        With CType(tkn.ObjTkn, TClass)
                            sw.Write(.NameVar)

                        End With

                    ElseIf TypeOf tkn.ObjTkn Is TVariable Then
                        With CType(tkn.ObjTkn, TVariable)
                            sw.Write(.NameVar)

                        End With

                    ElseIf TypeOf tkn.ObjTkn Is String Then
                        sw.Write(CType(tkn.ObjTkn, String))

                    Else
                        Debug.Print("{0}", tkn.ObjTkn.GetType())

                    End If

                Case Else
                    If txt.Length = 1 Then
                        Select Case txt(0)
                            Case "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c
                                sw.Write(txt)
                            Case Else
                                sw.Write(" " + txt + " ")
                        End Select
                    Else
                        If start_of_line Then
                            sw.Write(txt + " ")
                        Else
                            sw.Write(" " + txt + " ")
                        End If
                    End If

            End Select

            start_of_line = (tkn.TypeTkn = EToken.NL OrElse tkn.TypeTkn = EToken.Tab)
        Next

        Return sw.ToString()
    End Function

    Public Function IsReserved(e As EToken, lang As ELanguage) As Boolean
        Select Case e
            Case EToken.Public_, EToken.Class_, EToken.Function_, EToken.As_, EToken.Return_, EToken.EndFunction, EToken.EndClass
                Return True
            Case EToken.Sub_, EToken.EndSub, EToken.Of_, EToken.Shared_, EToken.Struct, EToken.Extends, EToken.EndStruct, EToken.New_
                Return True
            Case EToken.Abstract, EToken.Virtual, EToken.Override, EToken.Enum_, EToken.End_, EToken.Const_, EToken.Imports_, EToken.Operator_, EToken.Var
                Return True
            Case EToken.EndOperator, EToken.If_, EToken.Then_, EToken.EndIf_, EToken.Interface_, EToken.EndInterface, EToken.For_, EToken.To_, EToken.Step_, EToken.Next_, EToken.ReDim_, EToken.Each_
                Return True
            Case EToken.In_, EToken.Instanceof, EToken.Is_, EToken.CType_, EToken.ElseIf_, EToken.Select_, EToken.Switch, EToken.Case_, EToken.Break_, EToken.EndSelect, EToken.Else_, EToken.With_, EToken.IsNot_
                Return True
            Case EToken.EndWith, EToken.ExitSub, EToken.From_, EToken.MustOverride_, EToken.Do_, EToken.While_, EToken.ExitDo, EToken.Loop_, EToken.Throw_, EToken.Try_, EToken.Catch_, EToken.EndTry
                Return True
            Case EToken.ExitFor, EToken.AddressOf_, EToken.Base, EToken.Where_, EToken.GetType_, EToken.Iterator_, EToken.Yield_, EToken.Aggregate_, EToken.Into_, EToken.ParamArray_
                Return True

            Case EToken.Not_, EToken.MOD_
                If lang = ELanguage.Basic Then
                    Return True
                End If

            Case EToken.LP, EToken.RP, EToken.Comma, EToken.Eq, EToken.Dot, EToken.MUL, EToken.ADD, EToken.Mns, EToken.And_, EToken.NE, EToken.OR_, EToken.DIV, EToken.LE
            Case EToken.LC, EToken.RC, EToken.ASN, EToken.ADDEQ, EToken.LT, EToken.GT, EToken.GE, EToken.MULEQ, EToken.LB, EToken.RB, EToken.SUBEQ, EToken.SemiColon, EToken.Colon

            Case EToken.Unknown, EToken.NL, EToken.Tab, EToken.Comment
            Case EToken.Int, EToken.Char_, EToken.String_

            Case Else
                Debug.Assert(False)
        End Select

        Return False
    End Function

    Public Function TokenListToHTML(parser As TSourceParser, v As List(Of TToken)) As String
        Dim sw As New TStringWriter, start_of_line As Boolean = True

        For Each tkn In v
            Dim txt As String = ""

            Dim style As String

            'If tkn.TypeTkn = EToken.Public_ Then
            '    Debug.Print("")
            'End If
            If IsReserved(tkn.TypeTkn, parser.LanguageSP) Then
                style = "class=""reserved"""
            Else
                style = "class=""text"""

            End If

            Select Case tkn.TypeTkn
                Case EToken.Int
                Case EToken.NL
                Case EToken.Unknown
                Case EToken.Comment
                Case EToken.Tab
                Case Else
                    If parser.vTknName.ContainsKey(tkn.TypeTkn) Then
                        txt = parser.vTknName(tkn.TypeTkn)
                    Else
                        txt = "未登録語 : " + tkn.TypeTkn.ToString()
                        parser.vTknName.Add(tkn.TypeTkn, txt)
                        Debug.Print(txt)
                        '                        Debug.Assert(False)
                    End If

            End Select

            Select Case tkn.TypeTkn
                Case EToken.Int
                    sw.Write(tkn.StrTkn)

                Case EToken.NL
                    sw.WriteLine("")

                Case EToken.Tab
                    Dim k As Integer
                    For k = 0 To tkn.TabTkn * 4 - 1
                        sw.Write(" ")
                    Next

                Case EToken.Comment
                    If parser.LanguageSP = ELanguage.Basic Then
                        sw.Write("<span class=""comment"">{0}</span>", "'" + tkn.StrTkn)
                    Else
                        sw.Write("<span class=""comment"">{0}</span>", "//" + tkn.StrTkn)
                    End If

                Case EToken.As_, EToken.To_, EToken.Is_, EToken.IsNot_, EToken.In_, EToken.Into_, EToken.Where_, EToken.Take_, EToken.Step_, EToken.Implements_, EToken.ParamArray_
                    sw.Write("<span class=""reserved"">{0}</span>", " " + txt + " ")

                Case EToken.Then_
                    sw.Write("<span class=""reserved"">{0}</span>", " " + txt)

                Case EToken.Char_, EToken.String_
                    sw.Write("<span class=""string"">{0}</span>", tkn.StrTkn)

                Case EToken.Unknown
                    If TypeOf tkn.ObjTkn Is TDot Then
                        With CType(tkn.ObjTkn, TDot)
                            sw.Write("<span class=""symbol"">.</span><span class=""reference"">{0}</span>", parser.TranslageReferenceName(CType(tkn.ObjTkn, TDot)))

                        End With

                    ElseIf TypeOf tkn.ObjTkn Is TReference Then
                        With CType(tkn.ObjTkn, TReference)
                            sw.Write("<span class=""reference"">{0}</span>", parser.TranslageReferenceName(CType(tkn.ObjTkn, TReference)))
                        End With

                    ElseIf TypeOf tkn.ObjTkn Is TClass Then
                        With CType(tkn.ObjTkn, TClass)
                            sw.Write("<span class=""class"">{0}</span>", .NameVar)

                        End With

                    ElseIf TypeOf tkn.ObjTkn Is TVariable Then
                        With CType(tkn.ObjTkn, TVariable)
                            sw.Write("<span class=""variable"">{0}</span>", .NameVar)

                        End With

                    ElseIf TypeOf tkn.ObjTkn Is String Then
                        sw.Write("<span class=""text"">{0}</span>", CType(tkn.ObjTkn, String))

                    Else
                        Debug.Print("{0}", tkn.ObjTkn.GetType())

                    End If

                Case Else
                    If txt.Length = 1 Then
                        Select Case txt(0)
                            Case "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c
                                sw.Write("<span class=""symbol"">{0}</span>", txt)
                            Case Else
                                sw.Write("<span class="""">{0}</span>", " " + txt + " ")
                        End Select
                    Else
                        If start_of_line Then
                            sw.Write("<span {0}>{1}</span>", style, txt + " ")
                        Else
                            sw.Write("<span {0}>{1}</span>", style, " " + txt + " ")
                        End If
                    End If

            End Select

            start_of_line = (tkn.TypeTkn = EToken.NL OrElse tkn.TypeTkn = EToken.Tab)
        Next

        Return sw.ToString()
    End Function

    Public Shared Function FileExtension(lang As ELanguage) As String
        Select Case lang
            Case ELanguage.Basic
                Return ".vb"
            Case ELanguage.TypeScript
                Return ".ts"
            Case ELanguage.JavaScript
                Return ".js"
            Case ELanguage.Java
                Return ".java"
            Case Else
                Debug.Assert(False)
                Return Nothing
        End Select
    End Function

    ' Basicのソースを作る
    Public Sub MakeAllSourceCode(parser As TSourceParser)
        SetTokenListClsAll(parser)

        Dim out_dir As String = Path.GetFullPath(String.Format("{0}\{1}\{2}", ProjectHome, OutputDirectory, parser.LanguageSP))
        TDirectory.CreateDirectory(out_dir)

        Dim html_out_dir As String = out_dir + "\html"
        TDirectory.CreateDirectory(html_out_dir)

        Dim html_head As String = "<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">" + vbCr + vbLf + "<html xmlns=""http://www.w3.org/1999/xhtml"" >" + vbCr + vbLf + "<head>" + vbCr + vbLf + "<meta charset=""utf-8"" />" + vbCr + vbLf + "<title>Untitled Page</title>" + vbCr + vbLf + "<style type=""text/css"">" + vbCr + vbLf + ".reserved {" + vbCr + vbLf + vbTab + "color: blue;" + vbCr + vbLf + "}" + vbCr + vbLf + ".class {" + vbCr + vbLf + vbTab + "color: Teal;" + vbCr + "" + vbLf + "}" + vbCr + vbLf + ".string {" + vbCr + vbLf + vbTab + "color: red;" + vbCr + vbLf + "}" + vbCr + vbLf + ".comment {" + vbCr + vbLf + vbTab + "color: #008000;" + vbCr + vbLf + "}" + vbCr + vbLf + "</style>" + vbCr + vbLf + "</head>" + vbCr + vbLf + "<body>"

        '  すべてのソースに対し
        For Each src_f In SrcPrj
            CurSrc = src_f

            Dim navi_make_source_code As New TNaviMakeSourceCode(Me, parser)
            navi_make_source_code.NaviSourceFile(src_f)

            Dim src_path As String = String.Format("{0}\{1}{2}", out_dir, TPath.GetFileNameWithoutExtension(src_f.FileSrc), FileExtension(parser.LanguageSP))
            Dim src_txt As String = TokenListToString(parser, src_f.TokenListSrc)
            TFile.WriteAllText(src_path, src_txt)

            Dim html_path As String
            Dim html_txt As String

            html_path = String.Format("{0}\{1}.html", html_out_dir, TPath.GetFileNameWithoutExtension(src_f.FileSrc))
            html_txt = TokenListToHTML(parser, src_f.TokenListSrc)
            TFile.WriteAllText(html_path, html_head + "<pre><code>" + html_txt + "</code></pre></body></html>")

            For Each cls In (From c In src_f.ClaSrc Where AppClassList.Contains(c))
                For Each fnc In cls.FncCla
                    html_path = String.Format("{0}\{1}.html", html_out_dir, fnc.FullName())
                    html_txt = TokenListToHTML(parser, fnc.TokenListVar)
                    TFile.WriteAllText(html_path, html_head + "<pre><code>" + html_txt + "</code></pre></body></html>")
                Next
            Next

            CurSrc = Nothing
        Next

        If ApplicationClassList IsNot Nothing Then

            Dim class_list = From c In ApplicationClassList Where c.GenTokenListCls IsNot Nothing
            If class_list.Any() Then

                Dim sw As New StringWriter

                For Each cls1 In class_list
                    Select Case parser.LanguageSP
                        Case ELanguage.Basic
                            sw.WriteLine("Partial Public Class {0}", cls1.NameVar)
                            sw.WriteLine(TokenListToString(parser, cls1.GenTokenListCls))
                            sw.WriteLine("End Class")
                            sw.WriteLine()

                        Case ELanguage.JavaScript
                            sw.WriteLine(TokenListToString(parser, cls1.GenTokenListCls))

                    End Select
                Next

                Dim src_path As String = String.Format("{0}\{1}{2}", out_dir, "Generated", FileExtension(parser.LanguageSP))
                TFile.WriteAllText(src_path, sw.ToString())
            End If
        End If

    End Sub

    Public Function WriteInheritanceHierarchy(class_sw As StringWriter, cls1 As TClass) As Integer
        Dim indent As Integer

        If cls1.DirectSuperClassList.Count <> 0 Then
            indent = WriteInheritanceHierarchy(class_sw, cls1.DirectSuperClassList(0))
        Else
            indent = 0
        End If

        class_sw.WriteLine("<p style=""text-indent:{0}em""><a href=""../{1}/{1}.html"">{1}</a></p>", indent * 2, cls1.NameVar)

        Return indent + 1
    End Function

    Public Shared Function HexString(s As String) As String
        Dim sw As New StringWriter

        For Each c In s
            If AscW(c) < 256 Then
                sw.Write(c)
            Else
                sw.Write(String.Format("_{0:X4}", AscW(c)))
            End If
        Next

        Return sw.ToString()
    End Function

    Public Shared Function GetHtmlFileName(cls_fld_fnc As TVariable) As String
        If TypeOf cls_fld_fnc Is TClass OrElse TypeOf cls_fld_fnc Is TField Then
            Return HexString(cls_fld_fnc.NameVar)
        ElseIf TypeOf cls_fld_fnc Is TFunction Then
            Return cls_fld_fnc.IdxVar.ToString()
        Else
            Debug.Assert(False)
            Return Nothing
        End If
    End Function

    Public Sub CheckRefVar(ref1 As TReference)
        Dim fname As String

        If ref1.VarRef Is Nothing Then
            Debug.Assert(CurSrc IsNot Nothing)
            fname = TPath.GetFileNameWithoutExtension(CurSrc.FileSrc)
            Debug.Assert(fname = "@lib" OrElse fname = "System" OrElse fname = "sys" OrElse fname = "web")
        End If
    End Sub
End Class

Public Class TDelegatePair
    Public ClaDel As TClass
    Public FncDel As TFunction

    Public Sub New(cla1 As TClass, fnc1 As TFunction)
        ClaDel = cla1
        FncDel = fnc1
    End Sub
End Class


Public Class TFlowNode
    Public Shared CntNd As Integer
    Public IdxNd As Integer
    Public ToNd As New Dictionary(Of Object, TFlowNode)
End Class


Public Class TRefNode
    Inherits TFlowNode
    Public RefNode As TReference

    Public Sub New(ref1 As TReference)
        CntNd = CntNd + 1
        IdxNd = CntNd
        RefNode = ref1
    End Sub
End Class

Public Class TFncNode
    Inherits TFlowNode
    Public FncNode As TFunction

    Public Sub New(fnc1 As TFunction)
        CntNd = CntNd + 1
        IdxNd = CntNd
        FncNode = fnc1
    End Sub
End Class
