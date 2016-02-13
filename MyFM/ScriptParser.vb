Imports System.Diagnostics
'Imports InvariantBasicOrigin

'-------------------------------------------------------------------------------- TScriptParser
' C#の構文解析
Public Class TScriptParser
    Inherits TSourceParser

    Public vTkn As New Dictionary(Of String, EToken)
    Public CurBlc As TBlock
    Public CurPos As Integer
    Public CurTkn As TToken
    Public NxtTkn As TToken
    Dim EOTTkn As TToken
    Public CurVTkn As TList(Of TToken)
    Public CurLineIdx As Integer
    Dim CurLineStr As String

    Public Sub New(prj1 As TProject, lang As ELanguage)
        LanguageSP = lang
        ThisName = "this"
        SystemClassNameList = New List(Of String)() From {"byte", "char", "short", "int", "bool", "float", "double", "Object", "System", "string", "Type", "Exception", "Enumerable", "IList", "Math", "Attribute", "_Weak", "_Invariant", "_Parent", "_Prev", "_Next"}
        PrjParse = prj1
        RegTkn()

        TranslationTable.Add("System.True", "true")
        TranslationTable.Add("System.False", "false")
        TranslationTable.Add("Math.Ceiling", "ceiling")
        TranslationTable.Add("Math.Max", "max")
        TranslationTable.Add("Math.Min", "min")
        TranslationTable.Add("Math.Sqrt", "sqrt")
        TranslationTable.Add("Math.Abs", "abs")
        TranslationTable.Add("Math.Floor", "floor")
        TranslationTable.Add("Math.Round", "round")
        TranslationTable.Add("Math.Cos", "cos")
        TranslationTable.Add("Math.Sin", "sin")
    End Sub

    Public Overrides Sub ClearParse()
        CurBlc = Nothing
        CurPos = 0
        CurTkn = Nothing
        NxtTkn = Nothing
        CurVTkn = Nothing
        CurLineIdx = 0
        CurLineStr = ""
    End Sub

    Public Overrides Function NullName() As String
        If LanguageSP = ELanguage.JavaScript Then
            Return "undefined"
        Else
            Return "null"
        End If
    End Function

    Public Function GetTkn(type1 As EToken) As TToken
        Dim tkn1 As TToken

        If type1 = CurTkn.TypeTkn OrElse type1 = EToken.Unknown Then
            tkn1 = CurTkn

            CurPos += 1
            Do While CurPos < CurVTkn.Count AndAlso (CurVTkn(CurPos).TypeTkn = EToken.LineComment OrElse CurVTkn(CurPos).TypeTkn = EToken.BlockComment)
                CurPos += 1
            Loop

            If CurPos < CurVTkn.Count Then
                If CurVTkn(CurPos).TypeTkn = EToken.LowLine Then

                    CurLineIdx += 1
                    CurLineStr = PrjParse.CurSrc.vTextSrc(CurLineIdx)
                    CurVTkn = PrjParse.CurSrc.LineTkn(CurLineIdx)

                    CurPos = 0
                End If
                CurTkn = CurVTkn(CurPos)

                Dim nxt_pos As Integer = CurPos + 1
                Do While nxt_pos < CurVTkn.Count AndAlso (CurVTkn(nxt_pos).TypeTkn = EToken.LineComment OrElse CurVTkn(nxt_pos).TypeTkn = EToken.BlockComment)
                    nxt_pos += 1
                Loop

                If nxt_pos < CurVTkn.Count Then
                    NxtTkn = CurVTkn(nxt_pos)
                Else
                    NxtTkn = EOTTkn
                End If
            Else
                CurTkn = EOTTkn
                NxtTkn = EOTTkn
            End If

            'Debug.Print("token {0} {0}", CurTkn.StrTkn, CurTkn.TypeTkn)

            Return tkn1
        Else
            Chk(False, CurLineStr)
            Return Nothing
        End If
    End Function

    ' ジェネリック型の構文解析
    Function ReadGenType(id1 As TToken) As TClass
        Dim tp1 As TClass, tp2 As TClass
        Dim vtp As TList(Of TClass)

        GetTkn(EToken.LP)
        GetTkn(EToken.Of_)

        vtp = New TList(Of TClass)()
        Do While True
            tp2 = ReadType(False)
            vtp.Add(tp2)
            If CurTkn.TypeTkn <> EToken.Comma Then

                Exit Do
            End If
            GetTkn(EToken.Comma)
        Loop
        GetTkn(EToken.RP)

        ' ジェネリック型のクラスを得る。
        tp1 = PrjParse.GetAddSpecializedClass(id1.StrTkn, vtp)

        Return tp1
    End Function

    Function ReadType(is_new As Boolean) As TClass
        Dim tp1 As TClass
        Dim id1 As TToken, dim_cnt As Integer

        id1 = GetTkn(EToken.Id)
        If CurTkn.TypeTkn = EToken.LT Then
            ' ジェネリック型の場合

            ' ジェネリック型の構文解析
            tp1 = ReadGenType(id1)
        Else

            tp1 = PrjParse.GetCla(id1.StrTkn)
            If tp1 Is Nothing Then
                If id1.StrTkn = "Image" Then

                    tp1 = PrjParse.GetCla("HTMLImageElement")
                End If
                If tp1 Is Nothing Then

                    Throw New TError(String.Format("不明なクラス {0}", id1.StrTkn))
                End If
            End If
        End If
        If CurTkn.TypeTkn = EToken.LB AndAlso (NxtTkn.TypeTkn = EToken.RB OrElse NxtTkn.TypeTkn = EToken.Comma) Then
            GetTkn(EToken.LB)
            dim_cnt = 1
            Do While CurTkn.TypeTkn = EToken.Comma
                GetTkn(EToken.Comma)
                dim_cnt += 1
            Loop
            GetTkn(EToken.RB)
            If Not is_new Then
                tp1 = PrjParse.GetArrCla(tp1, dim_cnt)
            End If
        End If

        Return tp1
    End Function

    Function ReadTailCom() As String
        Dim tkn1 As TToken

        Select Case CurTkn.TypeTkn
            Case EToken.SemiColon
                Return ""

            Case EToken.LineComment
                tkn1 = GetTkn(EToken.LineComment)
                Return tkn1.StrTkn

            Case Else
                Debug.Assert(False)
                Return Nothing
        End Select
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

    Public Sub RegTkn()
        Dim dic1 As New Dictionary(Of String, EToken)

        EOTTkn = NewToken(EToken.EOT, "", 0)

        vTkn.Add("abstract", EToken.Abstract)

        dic1.Add("aggregate", EToken.Aggregate_)

        If LanguageSP = ELanguage.CSharp Then

            dic1.Add("as", EToken.As_)
        End If

        dic1.Add("base", EToken.Base)
        dic1.Add("break", EToken.Break_)
        dic1.Add("byval", EToken.ByVal_)
        dic1.Add("call", EToken.Call_)
        dic1.Add("case", EToken.Case_)
        dic1.Add("catch", EToken.Catch_)
        dic1.Add("class", EToken.Class_)

        dic1.Add("const", EToken.Const_)
        dic1.Add("constructor", EToken.Constructor)

        dic1.Add("default", EToken.Default_)
        dic1.Add("var", EToken.Var)
        dic1.Add("do", EToken.Do_)
        dic1.Add("each", EToken.Each_)
        dic1.Add("else", EToken.Else_)
        dic1.Add("elseif", EToken.ElseIf_)
        'dic1.Add("end", EToken.End_)
        dic1.Add("endif", EToken.EndIf_)
        dic1.Add("enum", EToken.Enum_)
        dic1.Add("exit", EToken.Exit_)

        dic1.Add("extends", EToken.Extends)

        dic1.Add("for", EToken.For_)
        dic1.Add("foreach", EToken.Foreach_)
        dic1.Add("from", EToken.From_)
        dic1.Add("function", EToken.Function_)
        dic1.Add("get", EToken.Get_)
        dic1.Add("goto", EToken.Goto_)
        dic1.Add("handles", EToken.Handles_)
        dic1.Add("if", EToken.If_)
        dic1.Add("implements", EToken.Implements_)

        Select Case LanguageSP
            Case ELanguage.CSharp
                dic1.Add("imports", EToken.Imports_)

            Case ELanguage.TypeScript, ELanguage.JavaScript, ELanguage.Java
                dic1.Add("import", EToken.Imports_)
        End Select

        dic1.Add("in", EToken.In_)
        dic1.Add("instanceof", EToken.Instanceof)
        dic1.Add("interface", EToken.Interface_)
        dic1.Add("into", EToken.Into_)
        dic1.Add("loop", EToken.Loop_)
        dic1.Add("namespace", EToken.Namespace_)
        dic1.Add("new", EToken.New_)
        dic1.Add("of", EToken.Of_)
        dic1.Add("out", EToken.Out_)

        dic1.Add("package", EToken.Package)

        dic1.Add("override", EToken.Override)
        dic1.Add("partial", EToken.Partial_)
        dic1.Add("private", EToken.Private_)
        dic1.Add("ref", EToken.Ref)
        dic1.Add("return", EToken.Return_)
        dic1.Add("select", EToken.Select_)
        If LanguageSP = ELanguage.CSharp Then

            dic1.Add("set", EToken.Set_)
        End If

        dic1.Add("static", EToken.Shared_)
        dic1.Add("struct", EToken.Struct)

        '        dic1.Add("sub", EToken.Sub_)
        dic1.Add("switch", EToken.Switch)
        dic1.Add("then", EToken.Then_)
        dic1.Add("throw", EToken.Throw_)
        dic1.Add("try", EToken.Try_)
        dic1.Add("using", EToken.Using_)
        dic1.Add("virtual", EToken.Virtual)
        dic1.Add("where", EToken.Where_)
        dic1.Add("while", EToken.While_)

        'dic1.Add("@else", EToken.PElse)
        'dic1.Add("@end", EToken.PEnd)
        'dic1.Add("@id", EToken.Id)
        'dic1.Add("@if", EToken.PIf)
        'dic1.Add("@int", EToken.Int)
        'dic1.Add("@hex", EToken.Hex)
        'dic1.Add("@set", EToken.PSet)

        dic1.Add("/*", EToken.BlockComment)
        dic1.Add("//", EToken.LineComment)

        'dic1.Add("<?", EToken.XMLST)
        'dic1.Add("<!", EToken.MATHST)
        'dic1.Add("]@", EToken.MATHED)

        dic1.Add("=", EToken.ASN)
        dic1.Add("+=", EToken.ADDEQ)
        dic1.Add("-=", EToken.SUBEQ)
        dic1.Add("*=", EToken.MULEQ)
        dic1.Add("/=", EToken.DIVEQ)
        dic1.Add("%=", EToken.MODEQ)

        dic1.Add("+", EToken.ADD)
        dic1.Add("-", EToken.Mns)
        dic1.Add("%", EToken.MOD_)
        dic1.Add("&", EToken.Anp)
        dic1.Add("(", EToken.LP)
        dic1.Add(")", EToken.RP)
        dic1.Add("*", EToken.MUL)
        dic1.Add(",", EToken.Comma)
        dic1.Add(".", EToken.Dot)
        dic1.Add("/", EToken.DIV)
        dic1.Add(":", EToken.Colon)
        dic1.Add(";", EToken.SemiColon)
        dic1.Add("?", EToken.Question)
        dic1.Add("[", EToken.LB)
        dic1.Add("]", EToken.RB)
        dic1.Add("^", EToken.HAT)
        dic1.Add("{", EToken.LC)
        dic1.Add("|", EToken.BitOR)
        dic1.Add("}", EToken.RC)
        dic1.Add("~", EToken.Tilde)

        dic1.Add("++", EToken.INC)
        dic1.Add("--", EToken.DEC)

        dic1.Add("==", EToken.Eq)
        dic1.Add("!=", EToken.NE)
        dic1.Add("<", EToken.LT)
        dic1.Add(">", EToken.GT)
        dic1.Add("<=", EToken.LE)
        dic1.Add(">=", EToken.GE)

        dic1.Add("||", EToken.OR_)
        dic1.Add("&&", EToken.And_)
        dic1.Add("!", EToken.Not_)

        'dic1.Add("->", EToken.RARROW)

        'dic1.Add("∀", EToken.ALL)
        'dic1.Add("∃", EToken.EXIST)
        'dic1.Add("∈", EToken.Element)
        'dic1.Add("∧", EToken.LAnd)
        'dic1.Add("∨", EToken.LOr)
        'dic1.Add("∩", EToken.INTERSECTION)
        'dic1.Add("∪", EToken.UNION)
        'dic1.Add("⊆", EToken.SUBSET)

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
        End If

        vTknName.Add(EToken.Abstract, "")
        If LanguageSP <> ELanguage.CSharp Then
            vTknName.Add(EToken.As_, ":")
        End If
        vTknName.Add(EToken.Is_, "==")
        vTknName.Add(EToken.IsNot_, "!=")
        vTknName.Add(EToken.Public_, "")
    End Sub

    Function NewToken(type1 As EToken, str1 As String, pos1 As Integer) As TToken
        Dim tkn1 As New TToken

        tkn1.TypeTkn = type1
        tkn1.StrTkn = str1
        tkn1.PosTkn = pos1

        Return tkn1
    End Function

    Public Overrides Function Lex(src_text As String) As TList(Of TToken)
        Dim v1 As New TList(Of TToken)
        Dim cur1 As Integer, spc As Integer
        Dim src_len As Integer
        Dim k1 As Integer
        Dim ch1 As Char
        Dim ch2 As Char
        Dim str1 As String = Nothing
        Dim type1 As EToken
        Dim prv_type As EToken
        Dim tkn1 As TToken
        Dim ok As Boolean
        Dim dmp As New TStringWriter

        src_len = src_text.Length
        v1 = New TList(Of TToken)()

        cur1 = 0
        prv_type = EToken.Unknown

        Do While True
            tkn1 = Nothing

            spc = 0
            Do While cur1 < src_len AndAlso Char.IsWhiteSpace(src_text(cur1))
                If src_text(cur1) = vbLf Then
                    dmp.WriteLine("")
                End If
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

            If Char.IsDigit(ch1) Then
                ' 数字の場合

                If ch1 = "0"c AndAlso ch2 = "x"c Then
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
                Else
                    ' 10進数の場合

                    For k1 = cur1 + 1 To src_text.Length - 1
                        ch2 = src_text(k1)
                        If Not Char.IsDigit(ch2) AndAlso ch2 <> "."c Then
                            Exit For
                        End If
                    Next
                    If k1 < src_text.Length AndAlso src_text(k1) = "f"c Then
                        k1 = k1 + 1
                    End If

                    str1 = TSys.Substring(src_text, cur1, k1)
                    tkn1 = NewToken(EToken.Int, str1, cur1)

                    cur1 = k1
                End If

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

            ElseIf ch1 = """"c OrElse ch1 = "'"c Then
                '  引用符の場合

                Dim quo1 As Char = ch1
                Dim sw As New TStringWriter
                For k1 = cur1 + 1 To src_len - 1
                    ch2 = src_text(k1)
                    If ch2 = quo1 Then
                        If ch2 = "'"c Then
                            tkn1 = New TToken(EToken.Char_, sw.ToString(), cur1)
                        Else
                            tkn1 = New TToken(EToken.String_, sw.ToString(), cur1)
                        End If
                        cur1 = k1 + 1
                        Exit For
                    End If

                    If ch2 = "\"c Then
                        Debug.Assert(k1 + 1 < src_len)
                        Select Case src_text(k1 + 1)
                            Case "n"c
                                sw.Write(vbLf)
                            Case "r"c
                                sw.Write(vbCr)
                            Case "t"c
                                sw.Write(vbTab)
                            Case "b"c
                                sw.Write(vbBack)
                            Case """"c
                                sw.Write(""""c)
                            Case "'"c
                                sw.Write("'"c)
                            Case "\"c
                                sw.Write("\"c)
                            Case "0"c
                                sw.Write(ChrW(0))
                            Case Else
                                Debug.Assert(False)
                        End Select
                        k1 += 1
                    Else
                        sw.Write(ch2)
                    End If
                Next

            ElseIf ch1 = "/"c AndAlso ch2 = "/"c Then
                ' 行コメントの場合

                k1 = src_text.IndexOf(vbLf, cur1)
                If k1 = -1 Then
                    k1 = src_len
                End If
                str1 = src_text.Substring(cur1, k1 - cur1)
                tkn1 = New TToken(EToken.LineComment, str1, cur1)
                cur1 = k1

            ElseIf ch1 = "/"c AndAlso ch2 = "*"c Then
                ' 複数行コメントの場合

                k1 = src_text.IndexOf("*/", cur1)
                Debug.Assert(k1 <> -1)
                str1 = src_text.Substring(cur1, k1 + 2 - cur1)
                tkn1 = New TToken(EToken.BlockComment, str1, cur1)
                cur1 = k1 + 2

            ElseIf ch1 = "@"c Then
                For k1 = cur1 + 1 To src_text.Length - 1
                    ch2 = src_text(k1)
                    If Not Char.IsLetterOrDigit(ch2) AndAlso ch2 <> "_"c Then
                        Exit For
                    End If
                Next
                str1 = src_text.Substring(cur1, k1 - cur1)
                Debug.Assert(str1 = "@_Weak" OrElse str1 = "@_Invariant" OrElse str1 = "@_Parent" OrElse str1 = "@_Prev" OrElse str1 = "@_Next")

                tkn1 = New TToken(EToken.Attribute, str1, cur1)
                cur1 += str1.Length

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

                        Debug.Print("lex str err [{0}]", TSys.Substring(src_text, cur1, cur1 + 2))
                        Chk(False)
                    End If
                End If

                tkn1 = NewToken(type1, str1, cur1)
                cur1 = cur1 + str1.Length
            End If

            ' Debug.WriteLine("token:{0} {1}", tkn1.StrTkn, tkn1.TypeTkn)
            tkn1.SpcTkn = spc
            v1.Add(tkn1)
            prv_type = tkn1.TypeTkn

            dmp.Write("{0}:{1} ", tkn1.TypeTkn, tkn1.StrTkn)
        Loop

        TFile.WriteAllText("C:\usr\prj\MyIDE\etc\TEST\lex.txt", dmp.ToString())

        Return v1
    End Function

    Public Overrides Sub RegAllClass(prj1 As TProject, src1 As TSourceFile)
        Dim id1 As TToken, k1 As Integer, cla1 As TClass, cla2 As TClass, id2 As TToken
        Dim v As TList(Of TToken) = src1.InputTokenList

        k1 = 0
        Do While k1 < v.Count
            Dim is_abstract As Boolean = False

            If v(k1).TypeTkn = EToken.Abstract Then
                is_abstract = True
                GetTkn(EToken.Abstract)
            End If

            Select Case v(k1).TypeTkn
                Case EToken.Delegate_, EToken.Class_, EToken.Struct, EToken.Interface_, EToken.Enum_

                    k1 += 1
                    Debug.Assert(v(k1).TypeTkn = EToken.Id)

                    id1 = v(k1)

                    If v(k1).TypeTkn = EToken.Delegate_ Then
                        Debug.Assert(prj1.GetCla(id1.StrTkn) Is Nothing)

                        cla1 = New TDelegate(prj1, id1.StrTkn)
                        prj1.SimpleParameterizedClassList.Add(cla1)
                        prj1.SimpleParameterizedClassTable.Add(cla1.NameCla(), cla1)
                    Else
                        cla1 = prj1.RegCla(id1.StrTkn)
                    End If

                    If k1 + 2 < v.Count AndAlso v(k1 + 1).TypeTkn = EToken.LT Then
                        cla1.GenericType = EGeneric.ParameterizedClass

                        cla1.GenCla = New TList(Of TClass)()

                        k1 += 2
                        Do While k1 < v.Count
                            id2 = v(k1)

                            cla2 = New TClass(prj1, id2.StrTkn)
                            cla2.IsParamCla = True
                            cla2.GenericType = EGeneric.ArgumentClass
                            cla1.GenCla.Add(cla2)

                            k1 += 1
                            If v(k1).TypeTkn = EToken.GT Then
                                Exit Do
                            End If

                            Debug.Assert(v(k1).TypeTkn = EToken.Comma)
                            k1 += 1
                        Loop

                        prj1.dicCmpCla.Add(cla1, New TList(Of TClass)())
                    Else
                        cla1.GenericType = EGeneric.SimpleClass
                    End If

                    Select Case cla1.NameCla()
                        Case "Object"
                            PrjParse.ObjectType = cla1
                        Case "System"
                            PrjParse.SystemType = cla1
                        Case "string"
                            PrjParse.StringType = cla1
                        Case "char"
                            PrjParse.CharType = cla1
                        Case "int"
                            PrjParse.IntType = cla1
                        Case "number"
                            PrjParse.DoubleType = cla1
                        Case "Type"
                            PrjParse.TypeType = cla1
                        Case "boolean"
                            PrjParse.BoolType = cla1
                    End Select

                    Debug.Print("クラス登録 {0}", cla1.LongName())
            End Select

            k1 += 1
        Loop
    End Sub

    Function ArgumentExpressionList(app1 As TApply) As TApply
        Dim trm1 As TTerm, right_token As EToken

        Select Case CurTkn.TypeTkn
            Case EToken.LP
                GetTkn(EToken.LP)
                right_token = EToken.RP

            Case EToken.LB
                GetTkn(EToken.LB)
                right_token = EToken.RB

            Case Else
                Debug.Assert(False)
        End Select

        If CurTkn.TypeTkn = EToken.Of_ Then
            GetTkn(EToken.Of_)
        End If
        '                 b_of = true;
        If CurTkn.TypeTkn <> right_token Then
            Do While True
                trm1 = TermExpression()
                app1.AddInArg(trm1)

                If CurTkn.TypeTkn <> EToken.Comma Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If
        GetTkn(right_token)

        Return app1
    End Function

    Function CallExpression(trm1 As TTerm) As TTerm
        Do While CurTkn.TypeTkn = EToken.LP OrElse CurTkn.TypeTkn = EToken.LB
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
        GetTkn(EToken.LB)
        If CurTkn.TypeTkn <> EToken.RB Then
            Do While True
                trm1 = TermExpression()
                arr1.TrmArr.Add(trm1)
                If CurTkn.TypeTkn = EToken.RB Then
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If
        GetTkn(EToken.RB)

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
        If CurTkn.TypeTkn = EToken.LB Then
            ' 配列の場合
            app1.IniApp = ArrayExpression()

            ' 配列型に変える
            app1.NewApp = PrjParse.GetArrCla(app1.NewApp, 1)
        End If
        If CurTkn.TypeTkn = EToken.From_ Then
            GetTkn(EToken.From_)

            Debug.Assert(CurTkn.TypeTkn = EToken.LB)
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
                aggr1.FunctionAggr = EAggregateFunction.Sum
            Case "Max"
                aggr1.FunctionAggr = EAggregateFunction.Max
            Case "Min"
                aggr1.FunctionAggr = EAggregateFunction.Min
            Case "Average"
                aggr1.FunctionAggr = EAggregateFunction.Average
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

            Case EToken.LB
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

            Case EToken.LT
                GetTkn(EToken.LT)
                type1 = ReadType(False)
                GetTkn(EToken.GT)
                trm1 = AdditiveExpression()
                trm1.CastType = type1

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

            Case Else
                Chk(False)
                Return Nothing
        End Select

        Return ret1
    End Function

    Function DotExpression() As TTerm
        Dim trm1 As TTerm
        Dim trm2 As TTerm
        Dim id1 As TToken

        trm1 = PrimaryExpression()

        Do While CurTkn.TypeTkn = EToken.Dot
            GetTkn(EToken.Dot)
            id1 = GetTkn(EToken.Id)
            trm2 = New TDot(trm1, id1.StrTkn)
            trm1 = CallExpression(trm2)
        Loop

        Return trm1
    End Function

    Function IncDecExpression() As TTerm
        Dim trm1 As TTerm = DotExpression()

        If CurTkn.TypeTkn = EToken.INC OrElse CurTkn.TypeTkn = EToken.DEC Then
            Dim tkn1 As TToken = GetTkn(EToken.Unknown)

            Return TApply.MakeApp1Opr(tkn1, trm1)
        End If

        Return trm1
    End Function


    Function UnaryExpression() As TTerm
        Dim tkn1 As TToken
        Dim trm1 As TTerm

        If CurTkn.TypeTkn = EToken.Mns Then
            tkn1 = GetTkn(EToken.Mns)
            trm1 = IncDecExpression()

            Return TApply.MakeApp1Opr(tkn1, trm1)
        End If

        Return IncDecExpression()
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
            Case EToken.Eq, EToken.ADDEQ, EToken.SUBEQ, EToken.MULEQ, EToken.DIVEQ, EToken.MODEQ, EToken.NE, EToken.LT, EToken.GT, EToken.LE, EToken.GE, EToken.Is_, EToken.IsNot_, EToken.Instanceof
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

    Function BitOrExpression() As TTerm
        Dim trm1 As TTerm
        Dim opr1 As TApply
        Dim type1 As EToken

        trm1 = NotExpression()
        If CurTkn.TypeTkn = EToken.BitOR Then

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

    Function AndExpression() As TTerm
        Dim trm1 As TTerm
        Dim opr1 As TApply
        Dim type1 As EToken

        trm1 = BitOrExpression()
        If CurTkn.TypeTkn = EToken.And_ OrElse CurTkn.TypeTkn = EToken.Anp Then

            type1 = CurTkn.TypeTkn
            opr1 = TApply.NewOpr(type1)
            opr1.AddInArg(trm1)
            Do While CurTkn.TypeTkn = type1
                GetTkn(type1)
                opr1.AddInArg(BitOrExpression())
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
            Case EToken.ASN, EToken.ADDEQ, EToken.SUBEQ, EToken.MULEQ, EToken.DIVEQ, EToken.MODEQ
                eq1 = GetTkn(EToken.Unknown)
                trm2 = CType(TermExpression(), TTerm)
                rel1 = TApply.NewOpr2(eq1.TypeTkn, trm1, trm2)
                asn1 = New TAssignment(rel1)

                Return asn1
        End Select

        Dim call1 As New TCall(CType(trm1, TApply))

        Return call1
    End Function

    Function ReadReturn(type_tkn As EToken) As TReturn
        Dim ret1 As TReturn

        GetTkn(type_tkn)
        If CurTkn.TypeTkn = EToken.SemiColon Then

            ret1 = New TReturn(Nothing, type_tkn = EToken.Yield_)
        Else
            ret1 = New TReturn(TermExpression(), type_tkn = EToken.Yield_)
        End If
        GetTkn(EToken.SemiColon)

        Return ret1
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

    Function ReadThrow() As TStatement
        Dim stmt1 As TThrow

        GetTkn(EToken.Throw_)
        stmt1 = New TThrow(CType(TermExpression(), TTerm))

        Return stmt1
    End Function

    Function ReadTry() As TStatement
        Dim try1 As New TTry

        GetTkn(EToken.Try_)

        try1.BlcTry = ReadBlock(try1)
        GetTkn(EToken.Catch_)
        try1.VarCatch = New TList(Of TVariable)()
        try1.VarCatch.Add(ReadVariable())
        try1.BlcCatch = ReadBlock(try1)

        Return try1
    End Function

    Function ReadDo() As TStatement
        Dim for1 As New TFor

        for1.IsDo = True

        GetTkn(EToken.Do_)
        GetTkn(EToken.While_)
        for1.CndFor = CType(TermExpression(), TTerm)

        for1.BlcFor = ReadBlock(for1)

        Return for1
    End Function

    Function ReadFor() As TStatement
        Dim for1 As New TFor
        Dim id1 As TToken

        GetTkn(EToken.For_)

        If CurTkn.TypeTkn = EToken.LP Then

            GetTkn(EToken.LP)
            GetTkn(EToken.Var)

            for1.IdxVarFor = ReadVariable()
            GetTkn(EToken.SemiColon)

            for1.CndFor = TermExpression()
            GetTkn(EToken.SemiColon)

            for1.StepStmtFor = AssignmentExpression()
            GetTkn(EToken.RP)
        Else

            GetTkn(EToken.Each_)

            If CurTkn.TypeTkn = EToken.Id Then

                id1 = GetTkn(EToken.Id)
                for1.InVarFor = New TLocalVariable(id1.StrTkn, Nothing)
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

            for1.InTrmFor = CType(TermExpression(), TTerm)
        End If

        for1.BlcFor = ReadBlock(for1)

        Return for1
    End Function

    Function ReadSelect() As TSelect
        Dim sel2 As New TSelect, case2 As TCase

        GetTkn(EToken.Switch)
        GetTkn(EToken.LP)
        sel2.TrmSel = TermExpression()
        GetTkn(EToken.RP)
        GetTkn(EToken.LC)

        Do While CurTkn.TypeTkn = EToken.Case_ OrElse CurTkn.TypeTkn = EToken.Default_
            case2 = New TCase()
            case2.DefaultCase = (CurTkn.TypeTkn = EToken.Default_)
            GetTkn(EToken.Unknown)

            If Not case2.DefaultCase Then

                Do While True
                    Dim trm1 As TTerm = TermExpression()
                    case2.TrmCase.Add(trm1)
                    If CurTkn.TypeTkn <> EToken.Comma Then
                        Exit Do
                    End If
                    GetTkn(EToken.Comma)
                Loop
            End If
            GetTkn(EToken.Colon)

            sel2.CaseSel.Add(case2)
            case2.BlcCase = ReadCaseBlock(sel2)

            If case2.DefaultCase Then
                Exit Do
            End If
        Loop

        GetTkn(EToken.RC)

        Return sel2
    End Function

    Function ReadIf() As TStatement
        Dim if2 As New TIf
        Dim if_cnd As TTerm

        GetTkn(EToken.If_)
        GetTkn(EToken.LP)
        if_cnd = CType(TermExpression(), TTerm)
        GetTkn(EToken.RP)

        Dim if_blc As New TIfBlock(if_cnd, ReadBlock(if2))
        if2.IfBlc.Add(if_blc)

        Do While CurTkn.TypeTkn = EToken.Else_

            GetTkn(EToken.Else_)
            If NxtTkn.TypeTkn = EToken.If_ Then
                GetTkn(EToken.If_)
                if_blc = New TIfBlock(TermExpression(), ReadBlock(if2))
                if2.IfBlc.Add(if_blc)
            Else
                if_blc = New TIfBlock(Nothing, ReadBlock(if2))
                if2.IfBlc.Add(if_blc)
                Exit Do
            End If
        Loop

        Return if2
    End Function

    Function ReadLocalVariableDeclaration() As TVariableDeclaration
        Dim stmt1 As New TVariableDeclaration
        Dim var1 As TVariable

        GetTkn(EToken.Var)
        stmt1.TypeStmt = EToken.VarDecl
        stmt1.ModDecl = New TModifier()
        Do While True
            var1 = ReadVariable()
            stmt1.VarDecl.Add(var1)
            CurBlc.VarBlc.Add(var1)
            If CurTkn.TypeTkn <> EToken.Comma Then
                Exit Do
            End If
            GetTkn(EToken.Comma)
        Loop

        stmt1.TailCom = ReadTailCom()
        GetTkn(EToken.SemiColon)

        Return stmt1
    End Function

    '  ブロックの構文解析をする
    Function ReadBlockSub(up1 As Object, case_block As Boolean) As TBlock
        Dim blc1 As New TBlock
        Dim blc_sv As TBlock

        blc_sv = CurBlc
        CurBlc = blc1

        If Not case_block Then
            GetTkn(EToken.LC)
        End If

        Do While CurTkn.TypeTkn <> EToken.RC AndAlso Not (case_block AndAlso CurTkn.TypeTkn = EToken.Case_)
            Dim stmt1 As TStatement = ReadStatement()
            If stmt1 Is Nothing Then
                Exit Do
            End If
            CurBlc.AddStmtBlc(stmt1)
        Loop

        If CurTkn.TypeTkn = EToken.RC Then

            GetTkn(EToken.RC)
        End If

        CurBlc = blc_sv

        Return blc1
    End Function

    '  ブロックの構文解析をする
    Function ReadBlock(up1 As Object) As TBlock
        Return ReadBlockSub(up1, False)
    End Function

    '  ブロックの構文解析をする
    Function ReadCaseBlock(up1 As Object) As TBlock
        Return ReadBlockSub(up1, True)
    End Function

    Function ReadStatement() As TStatement
        Dim mod1 As TModifier, stmt1 As TStatement = Nothing

        '  修飾子を調べる
        mod1 = ReadModifier()

        Select Case CurTkn.TypeTkn
            Case EToken.Var
                stmt1 = ReadLocalVariableDeclaration()

            Case EToken.If_
                stmt1 = ReadIf()

            Case EToken.Return_, EToken.Yield_
                stmt1 = ReadReturn(CurTkn.TypeTkn)

            Case EToken.Do_
                stmt1 = ReadDo()

            Case EToken.Switch
                stmt1 = ReadSelect()

            Case EToken.For_
                stmt1 = ReadFor()

            Case EToken.Exit_
                stmt1 = ReadExit()

            Case EToken.Id, EToken.Base, EToken.CType_, EToken.Dot
                stmt1 = AssignmentExpression()
                GetTkn(EToken.SemiColon)

            Case EToken.Try_
                stmt1 = ReadTry()

            Case EToken.Throw_
                stmt1 = ReadThrow()

            Case EToken.LineComment
                stmt1 = ReadLineComment()

            Case Else
                Chk(False)
        End Select

        Return stmt1
    End Function

    Function ReadVariableField(is_field As Boolean) As TVariable
        Dim var1 As TVariable
        Dim id1 As TToken
        Dim app1 As TApply

        If is_field Then
            var1 = New TField()
        Else
            var1 = New TLocalVariable()
        End If

        id1 = GetTkn(EToken.Id)
        var1.NameVar = id1.StrTkn

        If CurTkn.TypeTkn = EToken.Colon Then

            GetTkn(EToken.Colon)

            If CurTkn.TypeTkn = EToken.New_ Then
                app1 = NewExpression()
                var1.TypeVar = app1.NewApp
                var1.InitVar = app1

                Return var1
            End If

            var1.TypeVar = ReadType(False)
        End If

        If CurTkn.TypeTkn = EToken.ASN Then
            GetTkn(EToken.ASN)

            var1.InitVar = AdditiveExpression()
        End If

        Return var1
    End Function

    Function ReadVariable() As TVariable
        Return ReadVariableField(False)
    End Function

    Function ReadField() As TField
        Dim fld As TField = CType(ReadVariableField(True), TField)
        GetTkn(EToken.SemiColon)

        Return fld
    End Function

    Sub ReadFunctionArgument(arg_list As TList(Of TVariable))
        GetTkn(EToken.LP)

        Do While CurTkn.TypeTkn <> EToken.RP
            Dim by_ref As Boolean, param_array As Boolean
            Dim var1 As TVariable

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

            var1 = ReadVariable()
            var1.ByRefVar = by_ref
            var1.ParamArrayVar = param_array
            arg_list.Add(var1)

            If CurTkn.TypeTkn <> EToken.Comma Then
                Exit Do
            End If
            GetTkn(EToken.Comma)
        Loop

        GetTkn(EToken.RP)
    End Sub

    Function ReadFunction(cla1 As TClass, mod1 As TModifier) As TFunction
        Dim id1 As TToken, id2 As TToken, id3 As TToken
        Dim fnc1 As TFunction = Nothing

        Select Case CurTkn.TypeTkn
            Case EToken.Id
                id1 = GetTkn(EToken.Id)
                fnc1 = New TFunction(id1.StrTkn, Nothing)
                fnc1.TypeFnc = EToken.Function_

            Case EToken.Constructor
                id1 = GetTkn(EToken.Constructor)
                fnc1 = New TFunction("New@" + cla1.NameCla(), Nothing)
                fnc1.TypeFnc = EToken.New_

            Case EToken.Operator_
                GetTkn(EToken.Operator_)
                fnc1 = New TFunction(CurTkn.StrTkn, Nothing)
                fnc1.TypeFnc = EToken.Operator_
                fnc1.OpFnc = CurTkn.TypeTkn

            Case Else
                Debug.Assert(False)
        End Select

        fnc1.ModVar = mod1
        fnc1.ThisFnc = New TLocalVariable(ThisName, cla1)
        fnc1.IsNew = (fnc1.TypeFnc = EToken.New_)

        ReadFunctionArgument(fnc1.ArgFnc)

        If CurTkn.TypeTkn = EToken.Colon Then
            GetTkn(EToken.Colon)
            fnc1.RetType = ReadType(False)
        End If

        If CurTkn.TypeTkn = EToken.Implements_ Then
            GetTkn(EToken.Implements_)

            id2 = GetTkn(EToken.Id)
            GetTkn(EToken.Dot)
            id3 = GetTkn(EToken.Id)

            fnc1.InterfaceFnc = PrjParse.GetCla(id2.StrTkn)
            Debug.Assert(fnc1.InterfaceFnc IsNot Nothing)

            fnc1.ImplFnc = New TReference(id3.StrTkn)
        End If

        If fnc1.ModFnc().isMustOverride Then
            Return fnc1
        End If

        If cla1 Is Nothing OrElse cla1.KndCla <> EClass.InterfaceCla Then
            ' インターフェイスでない場合

            fnc1.BlcFnc = ReadBlock(fnc1)
        End If

        Return fnc1
    End Function

    Sub ReadClass(mod1 As TModifier)
        Dim cla1 As TClass, tkn1 As TToken
        Dim id1 As TToken

        PrjParse.dicGenCla.Clear()

        tkn1 = CurTkn
        GetTkn(EToken.Unknown)
        id1 = GetTkn(EToken.Id)
        If tkn1.TypeTkn = EToken.Delegate_ Then
            cla1 = PrjParse.GetDelegate(id1.StrTkn)
        Else
            cla1 = PrjParse.GetCla(id1.StrTkn)
        End If
        Debug.Assert(cla1 IsNot Nothing)
        cla1.ModVar = mod1

        PrjParse.CurSrc.ClaSrc.Add(cla1)

        Select Case tkn1.TypeTkn
            Case EToken.Delegate_
                cla1.KndCla = EClass.DelegateCla
            Case EToken.Class_
                cla1.KndCla = EClass.ClassCla
            Case EToken.Struct
                cla1.KndCla = EClass.StructCla
            Case EToken.Interface_
                cla1.KndCla = EClass.InterfaceCla
        End Select

        If cla1.GenCla IsNot Nothing Then
            ' ジェネリック クラスの場合

            ' for Add
            For Each cla_f In cla1.GenCla
                cla_f.IsParamCla = True
                PrjParse.dicGenCla.Add(cla_f.NameCla(), cla_f)
            Next

            GetTkn(EToken.LT)

            Do While True
                GetTkn(EToken.Id)
                If CurTkn.TypeTkn = EToken.GT Then
                    GetTkn(EToken.GT)
                    Exit Do
                End If
                GetTkn(EToken.Comma)
            Loop
        End If

        If tkn1.TypeTkn = EToken.Delegate_ Then
            Dim dlg1 As TDelegate = CType(cla1, TDelegate)

            ReadFunctionArgument(dlg1.ArgDlg)

            GetTkn(EToken.Colon)
            dlg1.RetDlg = ReadType(False)

            Dim fnc1 As New TFunction("Invoke", dlg1.RetDlg)
            fnc1.SetModFnc(mod1)
            fnc1.ArgFnc = dlg1.ArgDlg
            fnc1.ThisFnc = New TLocalVariable(ThisName, dlg1)
            fnc1.ClaFnc = dlg1
            dlg1.FncCla.Add(fnc1)
        Else
            If CurTkn.TypeTkn = EToken.Extends Then
                GetTkn(EToken.Extends)

                Dim spr_cla As TClass = ReadType(False)
                cla1.DirectSuperClassList.Add(spr_cla)
            End If

            If CurTkn.TypeTkn = EToken.Implements_ Then
                GetTkn(EToken.Implements_)

                Do While True

                    Dim spr_cla As TClass = ReadType(False)
                    cla1.InterfaceList.Add(spr_cla)

                    If CurTkn.TypeTkn <> EToken.Comma Then
                        Exit Do
                    End If
                    GetTkn(EToken.Comma)
                Loop
            End If

            If PrjParse.ObjectType IsNot cla1 AndAlso cla1.DirectSuperClassList.Count = 0 Then
                cla1.DirectSuperClassList.Add(PrjParse.ObjectType)
            End If

            GetTkn(EToken.LC)

            Do While CurTkn.TypeTkn <> EToken.RC
                Dim mod2 As TModifier = ReadModifier()

                If CurTkn.TypeTkn = EToken.Constructor Then
                    Dim fnc1 As TFunction = ReadFunction(cla1, mod2)
                    fnc1.ClaFnc = cla1
                    cla1.FncCla.Add(fnc1)
                Else

                    Debug.Assert(CurTkn.TypeTkn = EToken.Id)
                    Select Case NxtTkn.TypeTkn
                        Case EToken.Colon
                            Dim fld1 As TField = ReadField()
                            fld1.ModVar = mod2
                            cla1.AddFld(fld1)

                        Case EToken.LP
                            Dim fnc1 As TFunction = ReadFunction(cla1, mod2)
                            fnc1.ClaFnc = cla1
                            cla1.FncCla.Add(fnc1)

                        Case Else
                            Debug.Assert(False)
                    End Select
                End If

            Loop
            GetTkn(EToken.RC)
        End If

        PrjParse.dicGenCla.Clear()
        cla1.Parsed = True
        cla1.SrcCla = PrjParse.CurSrc
    End Sub

    Public Function ReadEnum() As TClass
        Dim cla1 As TClass
        Dim fld1 As TField
        Dim type1 As TClass

        GetTkn(EToken.Enum_)
        Dim id1 As TToken = GetTkn(EToken.Id)

        cla1 = PrjParse.GetCla(id1.StrTkn)
        Debug.Assert(cla1 IsNot Nothing)
        PrjParse.CurSrc.ClaSrc.Add(cla1)

        cla1.KndCla = EClass.EnumCla
        cla1.DirectSuperClassList.Add(PrjParse.ObjectType)
        type1 = cla1

        Do While CurTkn.TypeTkn = EToken.Id
            Dim ele1 As TToken = GetTkn(EToken.Id)
            fld1 = New TField(ele1.StrTkn, type1)
            cla1.AddFld(fld1)
        Loop
        GetTkn(EToken.RP)
        cla1.Parsed = True

        Return cla1
    End Function

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
                Case EToken.Attribute
                    If CurTkn.StrTkn = "@_Weak" Then
                        mod1.isWeak = True
                    ElseIf CurTkn.StrTkn = "@_Invariant" Then
                        mod1.isInvariant = True
                    ElseIf CurTkn.StrTkn = "@_Parent" Then
                        mod1.isParent = True
                    ElseIf CurTkn.StrTkn = "@_Prev" Then
                        mod1.isPrev = True
                    ElseIf CurTkn.StrTkn = "@_Next" Then
                        mod1.isNext = True
                    Else
                        Debug.Assert(False)
                    End If

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

    Sub ReadImports()
        Dim id1 As TToken
        Dim tkn1 As TToken
        Dim sb1 As TStringWriter

        GetTkn(EToken.Imports_)

        sb1 = New TStringWriter()
        Do While True
            id1 = GetTkn(EToken.Id)
            sb1.Append(id1.StrTkn)

            Select Case CurTkn.TypeTkn
                Case EToken.SemiColon
                    Exit Do
                Case EToken.Dot
                    tkn1 = GetTkn(EToken.Dot)
                    sb1.Append(tkn1.StrTkn)
                Case Else
                    Chk(False)
            End Select
        Loop

        PrjParse.CurSrc.vUsing.Add(sb1.ToString())
    End Sub

    Sub ReadModule(src1 As TSourceFile)
        Dim mod1 As TModifier
        Dim is_module As Boolean = False

        Do While CurTkn.TypeTkn = EToken.Imports_
            ReadImports()
        Loop

        If CurTkn.TypeTkn = EToken.Module_ Then
            is_module = True
            GetTkn(EToken.Module_)
            GetTkn(EToken.Id)
            GetTkn(EToken.LP)
        End If

        Do While True
            mod1 = ReadModifier()

            Do While CurTkn.TypeTkn = EToken.LineComment OrElse CurTkn.TypeTkn = EToken.BlockComment
                GetTkn(EToken.Unknown)
            Loop

            Select Case CurTkn.TypeTkn
                Case EToken.Delegate_, EToken.Class_, EToken.Struct, EToken.Interface_
                    ReadClass(mod1)

                Case EToken.Enum_
                    ReadEnum()

                Case EToken.EOT
                    Exit Do

                Case EToken.Function_
                    GetTkn(EToken.Function_)
                    Dim fnc1 As TFunction = ReadFunction(Nothing, mod1)
                    Debug.Assert(fnc1.NameVar = "_Weak" OrElse fnc1.NameVar = "_Parent" OrElse fnc1.NameVar = "_Prev" OrElse fnc1.NameVar = "_Next" OrElse fnc1.NameVar = "_Invariant")

                Case Else
                    Exit Do
            End Select
        Loop

        If is_module Then
            GetTkn(EToken.RP)
        End If
    End Sub

    Public Overrides Sub Parse(src1 As TSourceFile)
        CurVTkn = src1.InputTokenList
        CurPos = 0
        CurTkn = CurVTkn(CurPos)
        If CurPos + 1 < CurVTkn.Count Then
            NxtTkn = CurVTkn(CurPos + 1)
        Else
            NxtTkn = EOTTkn
        End If

        ReadModule(src1)
    End Sub
End Class

