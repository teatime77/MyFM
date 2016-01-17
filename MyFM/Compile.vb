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
        __SetParent(Me, Nothing)

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

        Dim navi_set_ref_stmt As New TNaviSetRefStmt
        navi_set_ref_stmt.NaviProject(Me)

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
                Dim dt As New TUseDefineAnalysis

                ' 参照パスをセットする。
                Dim set_dependency As New TNaviSetDependency
                set_dependency.NaviFunction(rule)

                Dim use_parent_class_list As List(Of TClass) = RefPath(rule)

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
                make_virtualizable_if_method.NaviFunction(rule)

                ' ナビゲート関数を作る。
                dt.UseParentClassList = use_parent_class_list
                dt.VirtualizableClassList = set_virtualizable_if.VirtualizableClassList
                MakeNaviFunctionList(rule, dt)

                For Each fnc1 In dt.NaviFunctionList
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


    '>>-------------------------------------------------------------------------------- ナビゲート関数を作る。

    Public Sub AddRuleCall(fnc1 As TFunction, cla1 As TClass)
        ' RuleのCall文を作る。
        Dim rule_fnc_list = From c In Sys.ThisAncestorSuperClassList(cla1).Distinct() From f In c.FncCla Where f.ModVar.isInvariant Select f
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

    Public Function InitNavigateFunction(function_name As String, cla1 As TClass, dt As TUseDefineAnalysis) As TFunction
        Dim fnc1 As New TFunction(function_name, Me, cla1)

        Debug.Print("Init Navigate Function {0}.{1}", cla1.NameVar, function_name)

        dt.NaviFunctionList.Add(fnc1)

        Dim self_var As New TLocalVariable("self", ObjectType)
        Dim app_var As New TLocalVariable("app", MainClass)
        fnc1.ArgFnc.Add(self_var)
        fnc1.ArgFnc.Add(app_var)
        fnc1.WithFnc = cla1

        Return fnc1
    End Function

    Public Sub MakeNaviFunctionList(self As Object, dt As TUseDefineAnalysis)
        If TypeOf self Is TFunction Then
            With CType(self, TFunction)
                Dim reachable_from_bottom_pending As New TList(Of TField)
                Dim reachable_from_bottom_processed As New List(Of TField)

                Dim reachable_from_top_pending As New TList(Of TField)
                Dim reachable_from_top_processed As New List(Of TField)

                For Each virtualizable_class In dt.VirtualizableClassList

                    ' virtualizable_classとそのスーパークラスのリスト
                    Dim virtualizable_super_class_list = Enumerable.Distinct(Sys.ThisAncestorSuperClassList(virtualizable_class))

                    ' 型がvirtualizable_classかスーパークラスであるフィールドのリスト
                    Dim parent_field_list = From parent_field In SimpleFieldList Where parent_field.ModVar.isStrong() AndAlso virtualizable_super_class_list.Contains(FieldElementType(parent_field))

                    ' reachable_from_bottom_pendingに入っていないparent_fieldを追加する。
                    reachable_from_bottom_pending.DistinctAddRange(parent_field_list)
                Next

                Dim parent_to_child_field_list_table As New Dictionary(Of TField, List(Of TField))

                Do While reachable_from_bottom_pending.Count <> 0
                    Dim current_field = reachable_from_bottom_pending.Pop()
                    reachable_from_bottom_processed.Add(current_field)

                    If Sys.ThisAncestorSuperClassList(MainClass).Contains(current_field.ClaFld) Then
                        ' current_fieldが属するクラスが、メインクラスかそのスーパークラスの場合

                        ' メインクラスからアクセス可能
                        reachable_from_top_pending.Add(current_field)
                    End If

                    ' current_fieldが属するクラスとそのスーパークラスのリスト
                    Dim current_field_super_class_list = Enumerable.Distinct(Sys.ThisAncestorSuperClassList(current_field.ClaFld))

                    ' 型がcurrent_fieldが属するクラスかスーパークラスであるフィールドのリスト
                    Dim parent_field_list = From parent_field In SimpleFieldList Where parent_field.ModVar.isStrong() AndAlso current_field_super_class_list.Contains(FieldElementType(parent_field))

                    For Each parent_field In parent_field_list
                        Dim child_field_list As List(Of TField)

                        If parent_to_child_field_list_table.ContainsKey(parent_field) Then
                            child_field_list = parent_to_child_field_list_table(parent_field)
                        Else
                            child_field_list = New List(Of TField)()
                            parent_to_child_field_list_table.Add(parent_field, child_field_list)
                        End If

                        ' このリストにcurrent_fieldを追加する。
                        child_field_list.Add(current_field)
                        Debug.Print("parent field {0}", parent_field)
                    Next

                    ' 未処理のフィールド
                    Dim not_processed_parent_field_list = From parent_field In parent_field_list Where Not reachable_from_bottom_processed.Contains(parent_field)

                    reachable_from_bottom_pending.DistinctAddRange(not_processed_parent_field_list)
                Loop

                Dim walked_field_list_table As New Dictionary(Of TClass, List(Of TField))

                Do While reachable_from_top_pending.Count <> 0
                    ' reachable_from_top_pendingからcurrent_fieldを取り出し、reachable_from_top_processedに追加する。
                    Dim current_field = reachable_from_top_pending.Pop()
                    reachable_from_top_processed.Add(current_field)

                    Dim walked_field_list As List(Of TField)
                    If walked_field_list_table.ContainsKey(current_field.ClaFld) Then
                        walked_field_list = walked_field_list_table(current_field.ClaFld)
                    Else
                        walked_field_list = New List(Of TField)()
                        walked_field_list_table.Add(current_field.ClaFld, walked_field_list)
                    End If
                    walked_field_list.Add(current_field)

                    If parent_to_child_field_list_table.ContainsKey(current_field) Then
                        Dim child_field_list As List(Of TField) = parent_to_child_field_list_table(current_field)

                        ' 未処理のフィールド
                        Dim not_processed_chile_field_list = From chile_field In child_field_list Where Not reachable_from_top_processed.Contains(chile_field)

                        reachable_from_top_pending.DistinctAddRange(not_processed_chile_field_list)
                    End If
                Loop

                Dim function_name As String = "Navigate_" + .NameVar

                Dim dummy_function As New TFunction(function_name, Nothing)
                Dim navi_needed_class_list As New TList(Of TClass)

                For Each cla1 In walked_field_list_table.Keys
                    Dim walked_field_list As List(Of TField) = walked_field_list_table(cla1)
                    Dim fnc1 As TFunction = InitNavigateFunction(function_name, cla1, dt)

                    If dt.UseParentClassList.Contains(cla1) Then
                        ' 親のフィールドの値を参照している場合

                        AddRuleCall(fnc1, cla1)
                    End If

                    Dim self_var As TVariable = fnc1.ArgFnc(0)
                    Dim app_var As TVariable = fnc1.ArgFnc(1)

                    Debug.Print("Rule {0}", cla1.NameVar)

                    For Each walked_field In walked_field_list
                        If walked_field.TypeVar.OrgCla IsNot Nothing Then
                            ' リストの場合

                            ' ループを作る。
                            Dim for1 As New TFor
                            for1.InVarFor = New TLocalVariable("x", Nothing)
                            for1.InTrmFor = New TDot(Nothing, walked_field)
                            for1.BlcFor = New TBlock()

                            ' リスト内の各要素に対しメソッドを呼ぶ。
                            Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TReference(for1.InVarFor), dummy_function))
                            app1.ArgApp.Add(New TReference(for1.InVarFor))
                            app1.ArgApp.Add(New TReference(app_var))
                            for1.BlcFor.AddStmtBlc(New TCall(app1))

                            fnc1.BlcFnc.AddStmtBlc(for1)

                            Debug.Print("For Each x in .{0}" + vbCrLf + "x.{1}()" + vbCrLf + "Next", walked_field.NameVar, function_name)
                        Else
                            ' リストでない場合

                            ' フィールドに対しメソッドを呼ぶ。
                            Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TDot(Nothing, walked_field), dummy_function))
                            app1.ArgApp.Add(New TDot(Nothing, walked_field))
                            app1.ArgApp.Add(New TReference(app_var))
                            fnc1.BlcFnc.AddStmtBlc(New TCall(app1))

                            Debug.Print(".{0}()", function_name)
                        End If

                        navi_needed_class_list.DistinctAdd(FieldElementType(walked_field))
                    Next

                    If Not dt.UseParentClassList.Contains(cla1) Then
                        ' 親のフィールドの値を参照していない場合

                        AddRuleCall(fnc1, cla1)
                    End If
                Next

                navi_needed_class_list.DistinctAddRange(dt.VirtualizableClassList)
                For Each cla1 In navi_needed_class_list
                    If Not walked_field_list_table.Keys.Contains(cla1) Then

                        Dim fnc1 As TFunction = InitNavigateFunction(function_name, cla1, dt)
                        AddRuleCall(fnc1, cla1)
                    End If
                Next
            End With
        End If
    End Sub
    '<<-------------------------------------------------------------------------------- ナビゲート関数を作る。

    ' 使用参照と定義参照の依存関係を求める。
    Public Sub VirtualizedMethodDefUseDependency(use_def As TUseDefineAnalysis)

        ' すべての仮想メソッドに対し
        For Each fnc1 In use_def.VirtualizedMethodList
            ' メソッド内の定義参照のリスト
            Dim def_list = From r In Sys.GetAllReference(fnc1.BlcFnc) Where r.DefRef

            ' メソッド内の定義参照に対し
            For Each ref1 In def_list

                ' ref1を含む文の余分な条件を取り除いた前提条件
                Dim cnd1 As TApply = Sys.GetTermPreConditionClean(ref1)

                ' メソッド内でref11と同じ変数に対する局所変数か自身のフィールドの定義参照のリスト
                Dim use_list = From r In Sys.GetAllReference(fnc1.BlcFnc) Where Sys.OverlapRefPath(ref1, r) AndAlso Not r.DefRef AndAlso (Not TypeOf r Is TDot OrElse CType(r, TDot).IsSelfField())

                For Each ref2 In use_list
                    ' ref2を含む文の余分な条件を取り除いた前提条件
                    Dim cnd2 As TApply = Sys.GetTermPreConditionClean(ref2)

                    ' 使用参照の文のAnd条件と定義参照の文のAnd条件が矛盾するなら、その使用参照は除外する。
                Next
            Next
        Next
    End Sub

    ' 親や子のフィールドの使用参照と定義参照の依存関係を求める。
    Public Sub VirtualizedMethodParentChildDefUseDependency(use_def As TUseDefineAnalysis)
        ' 仮想化メソッドのクラスのリスト
        Dim virtualized_all_class_list = From f In use_def.VirtualizedMethodList Select f.ClaFnc

        ' すべての仮想メソッドに対し
        For Each fnc1 In use_def.VirtualizedMethodList
            ' 仮想メソッド内のすべての参照のリストを得る。
            Dim parent_dot_list = From d In Sys.GetAllReference(fnc1.BlcFnc) Where TypeOf d Is TDot Select CType(d, TDot)

            ' クラス内のすべてのフィールドに対し
            For Each fld1 In Sys.AllFieldList(fnc1.ClaFnc)
                ' フィールドの型
                Dim element_type As TClass = FieldElementType(fld1)

                ' フィールドの型/スーパークラスで仮想化クラスのリスト
                Dim virtualizable_this_super_class_list = From c In virtualized_all_class_list Where Sys.DistinctThisAncestorSuperClassList(c).Contains(element_type)
                If virtualizable_this_super_class_list.Any() Then
                    ' フィールドの型/スーパークラスで仮想化クラスがある場合

                    ' フィールドの型のサブクラスで仮想化クラスのリスト
                    Dim virtualized_sub_class_list = Sys.DescendantSubClassList(element_type).Intersect(virtualized_all_class_list)

                    ' フィールドの型/スーパークラス/サブクラスで仮想化クラスのリスト
                    Dim virtualized_class_list As New TList(Of TClass)(virtualizable_this_super_class_list)
                    virtualized_class_list.AddRange(virtualized_sub_class_list)

                    ' フィールドの型/スーパークラス/サブクラスの仮想化クラスに対し
                    For Each cls1 In virtualized_class_list
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
                            Dim cnd1 As TApply = Sys.GetTermPreConditionClean(dot1)

                            ' 元の仮想メソッドで参照パスが共通の定義参照のリストを得る。
                            Dim child_parent_overlap_dot_list = From d In child_parent_dot_list Where Sys.OverlapRefPath(dot1, CType(d.UpTrm, TDot))
                            For Each dot2 In child_parent_overlap_dot_list


                                ' dot2を含む文の余分な条件を取り除いた前提条件
                                Dim cnd2 As TApply = Sys.GetTermPreConditionClean(dot2)

                                ' 前提条件cnd2で親のフィールド参照を自身のフィールド参照に変換する。
                                Dim nrm_cnd As TApply = Sys.NormalizeReference(Me, cnd2, fld1)

                                ' 使用参照の文のAnd条件と定義参照の文のAnd条件が矛盾するなら、その使用参照は除外する。
                            Next
                        Next

                        '-------------------------------------------------- 子のメソッドで定義した値を親のメソッドで使用する場合
                        ' 子の仮想メソッド内の定義参照のリスト
                        Dim child_def_dot_list = From d In child_dot_list Where d.DefRef AndAlso d.TrmDot Is Nothing

                        ' 子の仮想メソッド内の定義参照に対し
                        For Each dot1 In child_def_dot_list

                            ' dot1を含む文の余分な条件を取り除いた前提条件
                            Dim cnd1 As TApply = Sys.GetTermPreConditionClean(dot1)

                            ' 前提条件cnd1で親のフィールド参照を自身のフィールド参照に変換する。
                            Dim nrm_cnd As TApply = Sys.NormalizeReference(Me, cnd1, fld1)

                            ' 親の仮想メソッド内でfld1の使用参照のリスト
                            Dim parent_use_dot_list As List(Of TDot)

                            If fld1.TypeVar.OrgCla Is Nothing Then
                                ' フィールドがリストでない場合

                                ' 親の仮想メソッド内でfld1の使用参照のリストを求める。
                                parent_use_dot_list = (From d In parent_dot_list Where d.TrmDot Is Nothing AndAlso d.VarRef Is fld1 AndAlso Sys.OverlapRefPath(dot1, CType(d.UpTrm, TDot))).ToList()
                            Else
                                ' フィールドがリストの場合

                                ' 親の仮想メソッド内でfld1の使用参照のリストを求める。
                                parent_use_dot_list = (From d In parent_dot_list Where Sys.ChildElementDot(d, fld1) AndAlso Sys.OverlapRefPath(dot1, CType(d.UpTrm, TDot))).ToList()
                            End If

                            ' 親の仮想メソッド内でfld1の使用参照に対し
                            For Each dot2 In parent_use_dot_list

                                ' dot2を含む文の余分な条件を取り除いた前提条件
                                Dim cnd2 As TApply = Sys.GetTermPreConditionClean(dot2)

                                ' 使用参照の文のAnd条件と定義参照の文のAnd条件が矛盾するなら、その使用参照は除外する。
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
                Dim and1 As TApply = TApply.NewOpr(EToken.eAnd)
                and1.ArgApp.Add(cnd1)
                and1.ArgApp.AddRange(From c In TUseDefine.ThisAncestorChainList(child_chain) Select c.Cnd)

                If Sys.Consistent(and1) Then
                    ' 矛盾しない場合


                End If
            Next
        Next
    End Sub

    Public Function RefPath(rule As TFunction) As List(Of TClass)
        Dim result_class_list As New List(Of TClass)

        ' 関数内のフィールド参照のリストAndAlso Not TypeOf CType(d, TDot).TrmDot Is TDot
        Dim dot_list = From d In rule.RefFnc Where TypeOf d.VarRef Is TField AndAlso TypeOf d Is TDot Select CType(d, TDot)

        ' 関数内の代入のフィールド参照の最も内側のドットのリスト
        Dim def_inner_most_dot_list = From d In dot_list Where d.DefRef Select Sys.InnerMostDot(d)

        ' 関数内の値使用のフィールド参照の最も内側のドットのリスト
        Dim use_inner_most_dot_list = From d In dot_list Where Not d.DefRef Select Sys.InnerMostDot(d)

        ' 関数内の値使用のフィールド参照の最も内側のドットに対し
        For Each dot1 In use_inner_most_dot_list

            If dot1.TrmDot Is Nothing AndAlso dot1.VarRef.ModVar.isParent AndAlso TypeOf dot1.UpTrm Is TDot Then
                ' 親のフィールドを参照している場合

                Dim parent_dot As TDot = CType(dot1.UpTrm, TDot)

                ' パスが一致する代入のフィールド参照の最も内側のドットのリスト
                Dim equal_def_inner_most_dot_list = From d In def_inner_most_dot_list Where d.TrmDot Is Nothing AndAlso Sys.OverlapRefPath(d, parent_dot)

                If equal_def_inner_most_dot_list.Any() Then
                    ' 代入されるフィールドに対し、親のフィールドとして参照している場合

                    ' 親の不変条件を適用してから、子の不変条件を適用する必要がある。

                    ' 親のフィールドをメンバーとして持つクラスのリスト
                    Dim parent_field_class_list = Sys.ThisDescendantSubClassList(CType(parent_dot.VarRef, TField).ClaFld).Distinct()

                    ' 親のフィールドを参照するドットの処理対象クラスとそのスーパークラスのリスト
                    Dim super_class_list = Sys.ThisAncestorSuperClassList(dot1.TypeDot).Distinct()

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
        fnc1.ThisFnc = New TLocalVariable(ParsePrj.ThisName, cls1)
        fnc1.BlcFnc = New TBlock()
        fnc1.IsNew = False
        fnc1.IsTreeWalker = True
        fnc1.WithFnc = cls1

        Dim self_var As New TLocalVariable("self", ObjectType)
        fnc1.ArgFnc.Add(self_var)

        Dim parent_var As New TLocalVariable("_Parent", ObjectType)
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

            Dim super_class_list = From c In Sys.DistinctThisAncestorSuperClassList(cls1) Where c IsNot ObjectType

            Dim fnc1 As TFunction = MakeSetParentSub(set_parent_name, cls1)
            Dim self_var As TVariable = fnc1.ArgFnc(0)
            Dim parent_var As TVariable = fnc1.ArgFnc(1)

            function_list.Add(fnc1)

            Debug.Print("make set parent : {0}", cls1.NameVar)


            ' 親フィールドを得る。
            Dim parent_field_list = From c In super_class_list From f In c.FldCla Where f.ModVar.isParent Select f
            If parent_field_list.Any() Then
                ' 親フィールドがある場合

                Dim parent_field As TField = parent_field_list.First()

                ' 親フィールドに親の値を代入する。
                fnc1.BlcFnc.AddStmtBlc(New TAssignment(New TDot(Nothing, parent_field), New TReference(parent_var)))
            End If

            Dim strong_field_list = From c In super_class_list From f In c.FldCla Where f.ModVar.isStrong() AndAlso f.TypeVar.KndCla = EClass.eClassCla Select f
            For Each fld In strong_field_list
                If ApplicationClassList.Contains(fld.TypeVar) Then
                    ' フィールドの型が単純クラスの場合

                    If (From c In Sys.ThisAncestorSuperClassList(fld.TypeVar) From f In c.FldCla Where f.ModVar.isParent Select f).Any() Then

                        ' このフィールドの型およびその子孫のサブクラスで未処理のものを得る。
                        Dim pending_this_descendant_sub_class_list = From c In Sys.ThisDescendantSubClassList(fld.TypeVar) Where ApplicationClassList.Contains(c) AndAlso Not pending_class_list.Contains(c) AndAlso Not processed_class_list.Contains(c)

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
                    End If



                ElseIf fld.TypeVar.NameVar = "TList" Then
                    ' フィールドの型がリストの場合

                    Dim element_type = ElementType(fld.TypeVar)

                    If ApplicationClassList.Contains(element_type) Then

                        If (From c In Sys.ThisAncestorSuperClassList(element_type) From f In c.FldCla Where f.ModVar.isParent Select f).Any() Then

                            ' このフィールドの型およびその子孫のサブクラスで未処理のものを得る。
                            Dim pending_this_descendant_sub_class_list = From c In Sys.ThisDescendantSubClassList(element_type) Where ApplicationClassList.Contains(c) AndAlso Not pending_class_list.Contains(c) AndAlso Not processed_class_list.Contains(c)

                            ' 未処理のクラスのリストに追加する。
                            pending_class_list.AddRange(pending_this_descendant_sub_class_list)

                            Dim if1 As TIf = MakeNotNullIf(TApply.NewOpr2(EToken.eIsNot, New TDot(Nothing, fld), New TReference(ParsePrj.NullName())))

                            ' リストの親フィールドに親の値を代入する。
                            Dim list_parent_field As TField = (From f In fld.TypeVar.OrgCla.FldCla Where f.ModVar.isParent).First()
                            if1.IfBlc(0).BlcIf.AddStmtBlc(New TAssignment(New TDot(New TDot(Nothing, fld), list_parent_field), New TReference(self_var)))

                            ' 直前のフィールドのリストを得る。
                            Dim prev_field_list = From f In element_type.FldCla Where f.ModVar.isPrev
                            Dim prev_var As TVariable = Nothing

                            If prev_field_list.Any() Then
                                ' 直前のフィールドがある場合

                                ' 直前の値の作業変数(__prev)を宣言する。
                                Dim var_decl As New TVariableDeclaration
                                prev_var = New TLocalVariable("__prev", element_type)
                                var_decl.VarDecl.Add(prev_var)
                                if1.IfBlc(0).BlcIf.AddStmtBlc(var_decl)
                            End If

                            Dim for1 As New TFor
                            for1.InVarFor = New TLocalVariable("x", element_type)
                            for1.InTrmFor = New TDot(Nothing, fld)
                            for1.BlcFor = New TBlock()

                            If prev_field_list.Any() Then
                                ' 直前のフィールドがある場合

                                ' 直前のフィールドを得る。
                                Dim prev_field As TField = prev_field_list.First()

                                ' 直前のフィールドに作業変数(__prev)を代入する。
                                for1.BlcFor.AddStmtBlc(New TAssignment(New TDot(New TReference(for1.InVarFor), prev_field), New TReference(prev_var)))

                                ' 作業変数(__prev)を更新する。
                                for1.BlcFor.AddStmtBlc(New TAssignment(New TReference(prev_var), New TReference(for1.InVarFor)))
                            End If

                            Dim app1 As TApply = TApply.MakeAppCall(New TDot(New TReference(for1.InVarFor), dummy_function))
                            app1.ArgApp.Add(New TReference(for1.InVarFor))

                            ' 親の引数に値を入れる。
                            If OutputLanguageList(0) = ELanguage.JavaScript Then

                                ' 親はself
                                app1.ArgApp.Add(New TReference(self_var))
                            Else

                                ' 親はリスト
                                app1.ArgApp.Add(New TDot(Nothing, fld))
                            End If

                            for1.BlcFor.AddStmtBlc(New TCall(app1))
                            if1.IfBlc(0).BlcIf.AddStmtBlc(for1)

                            fnc1.BlcFnc.AddStmtBlc(if1)

                            If Not must_implement_class_list.Contains(element_type) Then
                                must_implement_class_list.Add(element_type)
                            End If

                            Debug.Print("make set parent : {0} {1}", cls1.NameVar, fld.NameVar)
                        End If

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
            If Sys.AncestorSuperClassList(fnc1.ClaFnc).Intersect(implemented_class_list_2).Any() Then
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


    ' -------------------------------------------------------------------------------- TNaviMakeVirtualizableIfMethod
    ' クラスの場合分けのIf文からクラスごとのメソッドを作る。
    Public Class TNaviMakeVirtualizableIfMethod
        Inherits TDeclarative
        Public VirtualizedMethodList As New TList(Of TFunction)

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

                        Debug.Print("Make Rule {0}.{1}", virtualizable_class.NameVar, fnc1.NameVar)

                        fnc1.__SetParent(fnc1, fnc1.ClaFnc.FncCla)
                        Dim set_parent_stmt As New TNaviSetParentStmt
                        set_parent_stmt.NaviFunction(fnc1, Nothing)

                        Dim set_up_trm As New TNaviSetUpTrm
                        set_up_trm.NaviFunction(fnc1, Nothing)

                        VirtualizedMethodList.Add(fnc1)
                    End If
                End With
            End If
        End Sub
    End Class
End Class