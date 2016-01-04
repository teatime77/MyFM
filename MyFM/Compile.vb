Imports System.IO
Imports System.Xml.Serialization
Imports System.Text
Imports System.Diagnostics

Partial Public Class TProject
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
            If Language = ELanguage.Basic Then
                src_f.vTextSrc = TFile.ReadAllLines(src_f.LibSrc.LibraryDirectory + "\" + src_f.FileSrc)
                src_f.LineTkn = New TList(Of TList(Of TToken))(From line1 In src_f.vTextSrc Select ParsePrj.Lex(line1))

            Else
                Dim src_text As String = TFile.ReadAllText(src_f.LibSrc.LibraryDirectory + "\" + src_f.FileSrc)
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
        __SetParent(Me, Nothing)

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
            For Each super_class In cls1.SuperClassList
                super_class.SubClassList.Add(cls1)
            Next
        Next

        ' 単純クラスのフィールドのリスト
        SimpleFieldList = (From cla1 In SimpleParameterizedClassList Where cla1.GenericType = EGeneric.SimpleClass From fld In cla1.FldCla Select fld).ToList()

        set_parent_stmt = New TNaviSetParentStmt()
        set_parent_stmt.NaviProject(Me, Nothing)

        set_up_trm = New TNaviSetUpTrm()
        set_up_trm.NaviProject(Me, Nothing)

        If MainClass IsNot Nothing Then
            Dim vrule = (From fnc In MainClass.FncCla Where fnc.ModVar.isInvariant).ToList()
            For Each rule In vrule
                ' 参照パスをセットする。
                'Dim set_ref_path As New TNaviSetRefPath
                'set_ref_path.NaviFunction(rule)
                Dim use_parent_class_list As List(Of TClass) = RefPath(rule)

                ' クラスの場合分けのIf文を探す。
                Dim set_classified_if As New TNaviSetClassifiedIf
                set_classified_if.NaviFunction(rule)

                ' クラスの場合分けのIf文からクラスごとのメソッドを作る。
                Dim make_classified_if_method As New TNaviMakeClassifiedIfMethod
                make_classified_if_method.NaviFunction(rule)

                ' ナビゲート関数を作る。
                Dim set_reachable_field As New TNaviMakeNavigateFunction
                set_reachable_field.Prj = Me
                set_reachable_field.UseParentClassList = use_parent_class_list
                set_reachable_field.ClassifiedClassList = make_classified_if_method.ClassifiedClassList
                set_reachable_field.NaviFunction(rule)

                For Each fnc1 In set_reachable_field.NaviFunctionList
                    EnsureFunctionIntegrity(fnc1)
                Next
            Next
        End If

        MakeSetParent()

        ' オーバーロード関数をセットする
        SetOvrFnc()

        If theMain IsNot Nothing Then

            ' 間接呼び出しをセットする
            SetCallAll()
        End If
    End Sub

    ' 最も内側のドットを返す。
    Public Function InnerMostDot(dot1 As TDot) As TDot
        If TypeOf dot1.TrmDot Is TDot Then
            Return InnerMostDot(CType(dot1.TrmDot, TDot))
        End If

        Return dot1
    End Function

    ' ドットの参照パスが重なるならTrueを返す。
    Public Function OverlapDotPath(dot1 As TDot, dot2 As TDot) As Boolean
        If dot1.VarRef IsNot dot2.VarRef Then
            ' 同じフィールドを指していない場合

            Return False
        End If

        If TypeOf dot1.UpTrm Is TDot AndAlso TypeOf dot2.UpTrm Is TDot Then
            ' 両方の親がドットの場合

            ' 再帰的に判断する。
            Return OverlapDotPath(CType(dot1.UpTrm, TDot), CType(dot2.UpTrm, TDot))
        Else
            ' 両方または片方の親がドットでない場合

            ' ドットの参照パスは重なっている。
            Return True
        End If
    End Function

    Public Function RefPath(rule As TFunction) As List(Of TClass)
        Dim result_class_list As New List(Of TClass)

        ' 関数内のフィールド参照のリストAndAlso Not TypeOf CType(d, TDot).TrmDot Is TDot
        Dim dot_list = From d In rule.RefFnc Where TypeOf d.VarRef Is TField AndAlso TypeOf d Is TDot Select CType(d, TDot)

        ' 関数内の代入のフィールド参照の最も内側のドットのリスト
        Dim def_inner_most_dot_list = From d In dot_list Where d.DefRef Select InnerMostDot(d)

        ' 関数内の値使用のフィールド参照の最も内側のドットのリスト
        Dim use_inner_most_dot_list = From d In dot_list Where Not d.DefRef Select InnerMostDot(d)

        ' 関数内の値使用のフィールド参照の最も内側のドットに対し
        For Each dot1 In use_inner_most_dot_list

            If dot1.TrmDot Is Nothing AndAlso dot1.VarRef.ModVar.isParent AndAlso TypeOf dot1.UpTrm Is TDot Then
                ' 親のフィールドを参照している場合

                Dim parent_dot As TDot = CType(dot1.UpTrm, TDot)

                ' パスが一致する代入のフィールド参照の最も内側のドットのリスト
                Dim equal_def_inner_most_dot_list = From d In def_inner_most_dot_list Where d.TrmDot Is Nothing AndAlso OverlapDotPath(d, parent_dot)

                If equal_def_inner_most_dot_list.Any() Then
                    ' 代入されるフィールドに対し、親のフィールドとして参照している場合

                    ' 親の不変条件を適用してから、子の不変条件を適用する必要がある。

                    ' 親のフィールドをメンバーとして持つクラスのリスト
                    Dim parent_field_class_list = TNaviUp.ThisDescendantSubClassList(CType(parent_dot.VarRef, TField).ClaFld).Distinct()

                    ' 親のフィールドを参照するドットの処理対象クラスとそのスーパークラスのリスト
                    Dim super_class_list = TNaviUp.ThisAncestorSuperClassList(dot1.TypeDot).Distinct()

                    ' 上記のスーパークラスの型のフィールドのリスト
                    Dim parent_field_list = From parent_field In Prj.SimpleFieldList Where parent_field.ModVar.isStrong() AndAlso super_class_list.Contains(Prj.FieldElementType(parent_field))

                    ' 親のフィールドを参照するドットの処理対象クラスの型のフィールドを持つクラスのリスト
                    Dim parent_field_class_list_2 = (From f In parent_field_list Select f.ClaFld).Distinct()

                    ' 親のフィールドをメンバーとして持ち、親のフィールドを参照するドットの処理対象クラスの型のフィールドを持つクラスの共通集合
                    Dim parent_field_class_list_3 = parent_field_class_list.Intersect(parent_field_class_list_2)

                    result_class_list.AddRange(parent_field_class_list_3)
                    result_class_list = result_class_list.Distinct().ToList()
                End If
            End If
        Next

        Return result_class_list
    End Function

    Public Shared Function MakeProject(project_path As String) As TProject
        'XmlSerializerオブジェクトを作成
        Dim serializer As New XmlSerializer(GetType(TProject))

        '読み込むファイルを開く
        Dim sr As New StreamReader(project_path, Encoding.UTF8)

        'XMLファイルから読み込み、逆シリアル化する
        Dim prj1 As TProject = CType(serializer.Deserialize(sr), TProject)
        'ファイルを閉じる
        sr.Close()

        For Each s In From x In prj1.LibraryList From y In x.SourceFileNameList Select y
            Debug.Print(s)
        Next

        If prj1.ClassNameTablePath <> "" Then
            prj1.ClassNameTable = TProgramTransformation.ReadClassNameTable(prj1.ClassNameTablePath, 2)
        End If

        prj1.Compile()

        Return prj1
    End Function


    Public Sub EnsureFunctionIntegrity(fnc1 As TFunction)
        fnc1.__SetParent(fnc1, fnc1.ClaFnc.FncCla)
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

        Dim set_up_trm As New TNaviSetUpTrm
        set_up_trm.NaviFunction(fnc1, Nothing)
    End Sub

    Public Function MakeSetParentSub(set_parent_name As String, cls1 As TClass) As TFunction
        Dim fnc1 As New TFunction(set_parent_name, Nothing)
        fnc1.ClaFnc = cls1
        fnc1.ModVar = New TModifier()
        fnc1.ModVar.isPublic = True
        fnc1.TypeFnc = EToken.eSub
        fnc1.ThisFnc = New TVariable(ParsePrj.ThisName, cls1)
        fnc1.BlcFnc = New TBlock()
        fnc1.IsNew = False
        fnc1.IsTreeWalker = True
        fnc1.WithFnc = cls1

        Dim self_var As New TVariable("self", ObjectType)
        fnc1.ArgFnc.Add(self_var)

        Dim parent_var As New TVariable("_Parent", ObjectType)
        fnc1.ArgFnc.Add(parent_var)

        Return fnc1
    End Function

    Public Function MakeNotNullIf(cnd As TTerm) As TIf
        Dim if1 As New TIf
        if1.IfBlc.Add(New TIfBlock(cnd, New TBlock()))

        Return if1
    End Function

    Public Sub MakeSetParent()
        If MainClass Is Nothing Then
            Exit Sub
        End If

        Dim set_parent_name As String = "__SetParent"
        Dim dummy_function As New TFunction(set_parent_name, Nothing)

        Dim pending_class_list As New TList(Of TClass)
        pending_class_list.Add(MainClass)

        Dim processed_class_list As New TList(Of TClass)

        Dim must_implement_class_list As New TList(Of TClass)

        Dim function_list As New List(Of TFunction)
        Do While pending_class_list.Count <> 0
            Dim cls1 As TClass = pending_class_list(0)
            pending_class_list.RemoveAt(0)

            processed_class_list.Add(cls1)

            Dim fnc1 As TFunction = MakeSetParentSub(set_parent_name, cls1)
            Dim self_var As TVariable = fnc1.ArgFnc(0)
            Dim parent_var As TVariable = fnc1.ArgFnc(1)

            function_list.Add(fnc1)

            Debug.Print("make set parent : {0}", cls1.NameVar)


            ' 親フィールドを得る。
            Dim parent_field_list = From f In cls1.FldCla Where f.ModVar.isParent
            If parent_field_list.Any() Then
                ' 親フィールドがある場合

                Dim parent_field As TField = parent_field_list.First()

                ' 親フィールドに親の値を代入する。
                fnc1.BlcFnc.AddStmtBlc(New TAssignment(New TDot(Nothing, parent_field), New TReference(parent_var)))
            End If

            Dim strong_field_list = From f In cls1.FldCla Where f.ModVar.isStrong() AndAlso f.TypeVar.KndCla = EClass.eClassCla
            For Each fld In strong_field_list
                If ApplicationClassList.Contains(fld.TypeVar) Then
                    ' フィールドの型が単純クラスの場合

                    ' このフィールドの型およびその子孫のサブクラスで未処理のものを得る。
                    Dim pending_this_descendant_sub_class_list = From c In TNaviUp.ThisDescendantSubClassList(fld.TypeVar) Where ApplicationClassList.Contains(c) AndAlso Not pending_class_list.Contains(c) AndAlso Not processed_class_list.Contains(c)

                    ' 未処理のクラスのリストに追加する。
                    pending_class_list.AddRange(pending_this_descendant_sub_class_list)

                    ' SetParentのCall文を作る。
                    Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TDot(Nothing, fld), dummy_function))
                    app1.AddInArg(New TDot(Nothing, fld))
                    app1.AddInArg(New TReference(self_var))

                    Dim if1 As TIf = MakeNotNullIf(TApply.NewOpr2(EToken.eIsNot, New TDot(Nothing, fld), New TReference(ParsePrj.NullName())))
                    if1.IfBlc(0).BlcIf.StmtBlc.Add(New TCall(app1))

                    fnc1.BlcFnc.AddStmtBlc(if1)

                    If Not must_implement_class_list.Contains(fld.TypeVar) Then
                        must_implement_class_list.Add(fld.TypeVar)
                    End If

                    Debug.Print("make set parent : {0} {1}", cls1.NameVar, fld.NameVar)

                ElseIf fld.TypeVar.NameVar = "TList" Then
                    ' フィールドの型がリストの場合

                    Dim element_type = ElementType(fld.TypeVar)

                    If ApplicationClassList.Contains(element_type) Then

                        ' このフィールドの型およびその子孫のサブクラスで未処理のものを得る。
                        Dim pending_this_descendant_sub_class_list = From c In TNaviUp.ThisDescendantSubClassList(element_type) Where ApplicationClassList.Contains(c) AndAlso Not pending_class_list.Contains(c) AndAlso Not processed_class_list.Contains(c)

                        ' 未処理のクラスのリストに追加する。
                        pending_class_list.AddRange(pending_this_descendant_sub_class_list)

                        Dim if1 As TIf = MakeNotNullIf(TApply.NewOpr2(EToken.eIsNot, New TDot(Nothing, fld), New TReference(ParsePrj.NullName())))

                        ' リストの親フィールドに親の値を代入する。
                        Dim list_parent_field As TField = (From f In fld.TypeVar.OrgCla.FldCla Where f.ModVar.isParent).First()
                        if1.IfBlc(0).BlcIf.AddStmtBlc(New TAssignment(New TDot(New TDot(Nothing, fld), list_parent_field), New TReference(self_var)))

                        Dim for1 As New TFor
                        for1.InVarFor = New TVariable("x", element_type)
                        for1.InTrmFor = New TDot(Nothing, fld)
                        for1.BlcFor = New TBlock()

                        Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TReference(for1.InVarFor), dummy_function))
                        app1.ArgApp.Add(New TReference(for1.InVarFor))
                        app1.ArgApp.Add(New TDot(Nothing, fld))
                        for1.BlcFor.AddStmtBlc(New TCall(app1))
                        if1.IfBlc(0).BlcIf.AddStmtBlc(for1)

                        fnc1.BlcFnc.AddStmtBlc(if1)

                        If Not must_implement_class_list.Contains(element_type) Then
                            must_implement_class_list.Add(element_type)
                        End If

                        Debug.Print("make set parent : {0} {1}", cls1.NameVar, fld.NameVar)
                    End If
                Else
                End If
            Next
        Loop

        ' 実装されたクラスのリスト
        Dim implemented_class_list = From f In function_list Select f.ClaFnc

        ' 実装が必要で未実装のクラスのリスト
        Dim not_implemented_class_list = From c In must_implement_class_list Where Not implemented_class_list.Contains(c)

        ' 実装が必要で未実装のクラスでメソッドを実装する。
        function_list.AddRange((From c In not_implemented_class_list Select MakeSetParentSub(set_parent_name, c)).ToList())

        ' 実装されたクラスのリストを再計算する。
        Dim implemented_class_list_2 = From f In function_list Select f.ClaFnc

        Dim i As Integer
        For i = function_list.Count - 1 To 0 Step -1
            Dim fnc1 As TFunction = function_list(i)
            If TNaviUp.AncestorSuperClassList(fnc1.ClaFnc).Intersect(implemented_class_list_2).Any() Then
                ' 先祖のクラスで実装されている場合

                If fnc1.BlcFnc.StmtBlc.Count = 0 Then
                    ' 実行文がない場合

                    function_list.RemoveAt(i)
                Else
                    ' 実行文がある場合

                    fnc1.ModVar.isOverride = True

                    ' Baseを呼ぶ
                    Dim app1 As New TApply
                    app1.TypeApp = EToken.eBaseCall
                    app1.FncApp = New TReference(set_parent_name)

                    app1.AddInArg(New TReference(fnc1.ArgFnc(0)))
                    app1.AddInArg(New TReference(fnc1.ArgFnc(1)))

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

        For Each fnc1 In function_list
            EnsureFunctionIntegrity(fnc1)
        Next
    End Sub


    ' -------------------------------------------------------------------------------- TNaviMakeClassifiedIfMethod
    ' クラスの場合分けのIf文からクラスごとのメソッドを作る。
    Public Class TNaviMakeClassifiedIfMethod
        Inherits TDeclarative
        Public ClassifiedClassList As New List(Of TClass)

        Public Function CopyAncestorBlock(if1 As TIf, blc1 As TBlock, cpy As TCopy) As TBlock
            Dim up_blc As TBlock = TDataflow.UpBlock(if1)
            Dim up_blc_copy As New TBlock

            ' up_blcの変数をup_blc_copyにコピーする。
            Dim vvar1 = From var1 In up_blc.VarBlc Select Sys.CopyVar(var1, cpy)
            up_blc_copy.VarBlc.AddRange(vvar1)

            ' up_blcの子の文をup_blc_copyにコピーする。
            up_blc_copy.StmtBlc.AddRange(From x In up_blc.StmtBlc Select CType(If(x Is if1, blc1, Sys.CopyStmt(x, cpy)), TStatement))

            If up_blc.ParentStmt Is if1.FunctionStmt Then
                ' メソッドの直下のブロックの場合

                Return up_blc_copy
            Else
                ' メソッドの直下のブロックでない場合

                ' １つ上のIf文を得る。
                Dim if_blc As TIfBlock = CType(TDataflow.UpStmtProper(up_blc.ParentStmt), TIfBlock)
                Dim if2 As TIf = CType(if_blc.ParentStmt, TIf)

                ' １つ上のif文を囲むブロックをコピーする。
                Return CopyAncestorBlock(if2, up_blc_copy, cpy)
            End If
        End Function

        Public Overrides Sub StartCondition(self As Object)
            If TypeOf self Is TIfBlock Then
                With CType(self, TIfBlock)
                    Dim if1 As TIf = CType(.ParentStmt, TIf)
                    If if1.ClassifiedIf Then

                        Dim fnc1 As New TFunction
                        Dim blc_if_copy As New TBlock
                        Dim cpy As New TCopy

                        cpy.CurFncCpy = fnc1

                        ' 関数のthisをコピーする。
                        fnc1.ThisFnc = Sys.CopyVar(.FunctionStmt.ThisFnc, cpy)

                        ' 関数の引数をコピーする。
                        Dim varg_var = From var1 In .FunctionStmt.ArgFnc Select Sys.CopyVar(var1, cpy)
                        fnc1.ArgFnc.AddRange(varg_var)

                        ' .BlcIfの変数をblc_if_copyにコピーする。
                        Dim vvar1 = From var1 In .BlcIf.VarBlc Select Sys.CopyVar(var1, cpy)
                        blc_if_copy.VarBlc.AddRange(vvar1)

                        ' このif文を囲むブロックの変数をコピーする。
                        Dim vvar2 = (From blc In TNaviUp.AncestorList(if1) Where TypeOf blc Is TBlock From var1 In CType(blc, TBlock).VarBlc Select Sys.CopyVar(var1, cpy)).ToList()

                        ' .BlcIfの子の文をblc_if_copyにコピーする。
                        blc_if_copy.StmtBlc.AddRange(From x In .BlcIf.StmtBlc Where Not x.ClassifiedIf Select Sys.CopyStmt(x, cpy))

                        ' このif文を囲むブロックをコピーする。
                        fnc1.BlcFnc = CopyAncestorBlock(if1, blc_if_copy, cpy)

                        ' クラスにメソッドを追加する。
                        Dim app1 As TApply = CType(.CndIf, TApply)
                        Dim ref1 As TReference = CType(app1.ArgApp(1), TReference)
                        Dim classified_class As TClass = CType(ref1.VarRef, TClass)

                        fnc1.NameVar = .FunctionStmt.NameVar
                        fnc1.ModVar = New TModifier()
                        fnc1.ModVar.isInvariant = True
                        fnc1.TypeFnc = .FunctionStmt.TypeFnc
                        fnc1.ClaFnc = classified_class
                        fnc1.ClaFnc.FncCla.Add(fnc1)
                        fnc1.WithFnc = classified_class

                        Debug.Print("Make Rule {0}.{1}", classified_class.NameVar, fnc1.NameVar)

                        fnc1.__SetParent(fnc1, fnc1.ClaFnc.FncCla)
                        Dim set_parent_stmt As New TNaviSetParentStmt
                        set_parent_stmt.NaviFunction(fnc1, Nothing)

                        Dim set_up_trm As New TNaviSetUpTrm
                        set_up_trm.NaviFunction(fnc1, Nothing)

                        ClassifiedClassList.Add(classified_class)
                    End If
                End With
            End If
        End Sub
    End Class
End Class