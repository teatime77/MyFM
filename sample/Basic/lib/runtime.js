var $BooleanType = function () { }
var $IntType = function () { }
var $DoubleType = function () { }
var $StringType = function () { }
var $ArrayType = function () { }

$BooleanType.prototype.$ClassName = "Boolean";
$IntType.prototype.$ClassName     = "Int";
$DoubleType.prototype.$ClassName  = "Double";
$StringType.prototype.$ClassName  = "string";
$ArrayType.prototype.$ClassName   = "Array";

var $SysTypeTable = {
    "Boolean": $BooleanType,
    "Int": $IntType,
    "Double": $DoubleType,
    "string": $StringType,
    "Array": $ArrayType
}


/*
    ログを出力する。
 */
var $txtLog;
function $Log() {
    var s = $Format.apply(null, arguments);
    if ($txtLog != undefined) {

        $txtLog.value += s + "\r\n";
    }
    else {
        console.log(s);
    }
}

/*
    ログをクリアする。
 */
function $LogClear() {
    if ($txtLog != undefined) {
        $txtLog.value = "";
    }
}

/**
 * フォーマット関数
 */
var $Format = function (fmt) {
    var rep_fn = undefined;

    var args = arguments;
    rep_fn = function (m, k) { return args[parseInt(k) + 1]; }

    return fmt.replace(/\{(\w+)\}/g, rep_fn);
}

/*
    名前からクラスを得る。
*/
function $GetClassByName(class_name) {
    var c = $SysTypeTable[class_name];

    if (c != undefined) {
        return c;
    }
    else {
        return eval(class_name);
    }
}

/*
    システムクラスならtrueを返す。
*/
function $IsSysClass(c) {
    var class_name = c.prototype.$ClassName;
    return class_name != undefined && $SysTypeTable[class_name] != undefined;
}

/*
    タブの数をスペースに変換する。
*/
function $TabSpace(n) {
    var s = "";
    for (i = 0; i < n; i++) {
        s += " ";
    }

    return s;
}

window.onload = function () {
    $txtLog = document.getElementById("$txtLog");

    var runtime = new TRuntime();
    runtime.App = new TMyApplication();
    runtime.Canvas = document.getElementById("canvassample");
    runtime.RuntimeInitialize();

    var txt = document.getElementById("app-xml").textContent;

    var lex1 = new TXmlParser(txt);
    runtime.App = lex1.Parse();

//    runtime.App.AppInitialize();

    if (runtime.App.__SetParent) {
        runtime.App.__SetParent(runtime.App, undefined);
    }

    runtime.Run();

    try {
        var ser = new TXmlSerializer();

        ser.SerializeTop(runtime.App);
        $Log(ser.Text);


        var lex = new TXmlParser(ser.Text);
        var app = lex.Parse();

        var ser2 = new TXmlSerializer();

        ser2.SerializeTop(app);
        $LogClear();
        $Log("検証2 -------------------------------------------------------------------------------");
        $Log(ser2.Text);
    }
    catch (e) {
        $Log("Error : {0}", e);
    }

    $Log("-------------------------------------------------------------------------------");



}

var TRuntime = function () {};

TRuntime.prototype.SetMouseEvent = function (ev) {
    console.log("SetMouseEvent");
    var rc = this.Canvas.getBoundingClientRect();
    var x = (window.pageXOffset !== undefined) ? window.pageXOffset : (document.documentElement || document.body.parentNode || document.body)["scrollLeft"];
    var y = (window.pageYOffset !== undefined) ? window.pageYOffset : (document.documentElement || document.body.parentNode || document.body)["scrollTop"];
    console.log("SetMouseEvent:" + rc.left + " " + window.pageXOffset + " " + document.documentElement.scrollLeft);
    this.App.MousePosition.X = ev.clientX - rc.left; // ev.x - (x + rc.left);
    this.App.MousePosition.Y = ev.clientY - rc.top; // ev.y - (y + rc.top);
};

TRuntime.prototype.SetTouchEvent = function (ev) {
    console.log("SetMouseEvent");
    var rc = this.Canvas.getBoundingClientRect();
    var x = (window.pageXOffset !== undefined) ? window.pageXOffset : (document.documentElement || document.body.parentNode || document.body)["scrollLeft"];
    var y = (window.pageYOffset !== undefined) ? window.pageYOffset : (document.documentElement || document.body.parentNode || document.body)["scrollTop"];
    console.log("SetMouseEvent:" + rc.left + " " + window.pageXOffset + " " + document.documentElement.scrollLeft);
    this.App.MousePosition.X = ev.changedTouches[0].pageX - rc.left; // ev.x - (x + rc.left);
    this.App.MousePosition.Y = ev.changedTouches[0].pageY - rc.top; // ev.y - (y + rc.top);
};

TRuntime.prototype.RuntimeInitialize = function () {
    var _this = this;
    this.App.Size.X = this.Canvas.width;
    this.App.Size.Y = this.Canvas.height;
    this.Graphics = new TGraphics(this.Canvas);
    this.Canvas.onmousedown = function (ev) {
        ev.preventDefault();
        //_this.SetMouseEvent(ev);
        //console.log("onmousedown:" + ev.clientX + " " + ev.clientY);
    };
    this.Canvas.onmouseenter = function (ev) {
        //this.SetMouseEvent(ev);
        //            console.log("onmouseenter");
    };
    this.Canvas.onmouseleave = function (ev) {
        //this.SetMouseEvent(ev);
        //console.log("onmouseleave");
    };
    this.Canvas.onmousemove = function (ev) {
        _this.SetMouseEvent(ev);
        //console.log("onmousemove:" + ev.x + " " + ev.y);
    };
    this.Canvas.onmouseout = function (ev) {
        //this.SetMouseEvent(ev);
        //console.log("onmouseout");
    };
    this.Canvas.onmouseover = function (ev) {
        //this.SetMouseEvent(ev);
        //console.log("onmouseover");
    };
    this.Canvas.onmouseup = function (ev) {
        //this.SetMouseEvent(ev);
        //console.log("onmouseup:" + ev.x + " " + ev.y);
    };
    this.Canvas.onmousewheel = function (ev) {
        //this.SetMouseEvent(ev);
        //console.log("onmousewheel");
    };
    this.Canvas.ontouchmove = function (ev) {
        ev.preventDefault(); // タッチによる画面スクロールを止める
        _this.SetTouchEvent(ev);
    };

    this.Canvas.addEventListener('touchmove', function (event) {
        event.preventDefault(); // タッチによる画面スクロールを止める
        _this.SetTouchEvent(ev);
    }, false);
};

TRuntime.prototype.AnimationFrameLoop = function () {
    var _this = this;
    this.Graphics.Clear();
    this.App.Graphics = this.Graphics;
    this.App.AllRule(this.App, this.App);

    this.Graphics.Context.setTransform(1, 0, 0, 1, 0, 0);

    if (this.App.ShapeList != null) {
        for (var i = 0; i < this.App.ShapeList.length; i++) {
            this.App.ShapeList[i].Draw(this.Graphics);
        }
    }

    if (this.App.MainControl != null) {
        this.App.MainControl.Draw(this.Graphics);
    }

    window.requestAnimationFrame(function () { return _this.AnimationFrameLoop(); });
};

TRuntime.prototype.Run = function () {
    this.AnimationFrameLoop();
};

function Inherits(sub_class, super_class) {

    for (var p in super_class) {
        if (super_class.hasOwnProperty(p) && !sub_class.hasOwnProperty(p)) {
            sub_class[p] = super_class[p];
        }
    }

    function __() {
        this.constructor = sub_class;
    }

    __.prototype = super_class.prototype;

    sub_class.prototype = new __();
    sub_class.prototype.SuperClass = super_class.prototype;
};

function CallInstanceInitializer(current, self) {
    if (current.SuperClass != undefined && current.SuperClass.__InstanceInitializer != undefined) {
        CallInstanceInitializer(current.SuperClass, self);
    }
    if (current.__InstanceInitializer != undefined) {
        current.__InstanceInitializer.call(self);
    }

}

function $Query(v, cnd, sel) {
    var obj = {
        Source: v,
        Condition: cnd,
        Selection: sel,
        Index: 0,
        Result: [],
        Done: false,

        next: function () {
            if (Array.isArray(this.Source)) {

                while (this.Index < this.Source.length) {
                    var val = this.Source[this.Index];
                    this.Index++;
                    if (this.Condition == undefined || this.Condition(val)) {
                        var res = (this.Selection == undefined ? val : this.Selection(val));
                        this.Result.push(res);
                        return { value: res, done: false };
                    }
                }
                this.Done = true;
                return { value: undefined, done: true };
            }
            else {
                while (true) {
                    var nxt = this.Source.next();
                    if (nxt.done) {
                        this.Done = true;
                        return nxt;
                    }
                    else {
                        if (this.Condition == undefined || this.Condition(nxt.value)) {
                            var res = (this.Selection == undefined ? nxt.value : this.Selection(nxt.value));
                            this.Result.push(res);
                            return { value: res, done: false };
                        }
                    }
                }
            }
        }
        ,
        ToArray: function () {
            while (!this.Done) {
                this.next();
            }

            return this.Result;
        }
        ,
        Count: function () {
            this.ToArray();

            return this.Result.length;
        }
        ,
        Any: function () {
            while (this.Result.length == 0 && !this.Done) {
                this.next();
            }

            return this.Result.length != 0;
        }
        ,
        First: function () {
            while (this.Result.length == 0 && !this.Done) {
                this.next();
            }

            if (this.Result.length != 0) {
                return this.Result[0];
            }
            else {
                return undefined;
            }
        }
        ,
        Contains: function (x) {
            if (this.Result.indexOf(x) != -1) {
                return true;
            }

            while (!this.Done) {
                var nxt = this.next();
                if (!nxt.done && nxt.value == x) {
                    return true;
                }
            }

            return false;
        }
        ,
        Sum: function () {
            this.ToArray();

            var n = 0;
            for (var i = 0; i < this.Result.length; i++) {
                n += this.Result[i];
            }
            return n;
        }
        ,
        Max: function () {
            this.ToArray();

            if (this.Result.length == 0) {
                return undefined;
            }
            var n = this.Result[0];
            for (var i = 1; i < this.Result.length; i++) {
                n = Math.max(n, this.Result[i]);
            }
            return n;
        }
        ,
        Min: function () {
            this.ToArray();

            if (this.Result.length == 0) {
                return undefined;
            }
            var n = this.Result[0];
            for (var i = 1; i < this.Result.length; i++) {
                n = Math.min(n, this.Result[i]);
            }
            return n;
        }
        ,
        Average: function () {
            this.ToArray();

            if (this.Result.length == 0) {
                return undefined;
            }
            var n = 0;
            for (var i = 0; i < this.Result.length; i++) {
                n += this.Result[i];
            }
            return n / this.Result.length;
        }
    };

    return obj;
}

var Double = function () {
}

Double.IsNaN = function (x) {
    return x == undefined;
}

var Debug = function () {
}

Debug.Assert = function (x, msg) {
    if (!x) {

        console.log("assert error!" + msg);
    }
}

//-------------------------------------------------------------------------------- TXmlParser
var TXmlParser = function (txt) {
    this.Text = txt;
    this.Pos = 0;
    this.Word = "";
}

TXmlParser.prototype.SkipWhite = function () {
    for (; this.Pos < this.Text.length; this.Pos++) {
        switch (this.Text[this.Pos]) {
            case ' ':
            case '\t':
            case '\r':
            case '\n':
                break;

            default:
                return;
        }
    }
}

TXmlParser.prototype.NextToken = function () {
    this.SkipWhite();
    this.WordType = undefined;

    if (this.Text.length <= this.Pos) {
        this.Word = undefined;
        return undefined;
    }

    var ch1 = this.Text[this.Pos];
    var ch2 = (this.Pos + 1 < this.Text.length ? this.Text[this.Pos + 1] : 0);
    var st = this.Pos;

    switch (ch1) {
        case '<':
            if (ch2 == '?') {
                this.Word = this.Text.substr(this.Pos, 2);
                this.Pos += 2;
            }
            else if (ch2 == '/') {
                this.Word = this.Text.substr(this.Pos, 2);
                this.Pos += 2;
            }
            else {
                this.Word = this.Text.substr(this.Pos, 1);
                this.Pos += 1;
            }
            break;

        case '/':
            if (ch2 == '>') {
                this.Word = this.Text.substr(this.Pos, 2);
                this.Pos += 2;
            }
            else {
                this.Word = this.Text.substr(this.Pos, 1);
                this.Pos += 1;
            }
            break;

        case '?':
            if (ch2 == '>') {
                this.Word = this.Text.substr(this.Pos, 2);
                this.Pos += 2;
            }
            else {
                this.Word = this.Text.substr(this.Pos, 1);
                this.Pos += 1;
            }
            break;

        case '=':
        case '>':
        case ':':
            this.Word = this.Text.substr(this.Pos, 1);
            this.Pos += 1;
            break;

        case '\"':
            this.Pos++;
            loop: for (; this.Pos < this.Text.length;) {
                switch (this.Text[this.Pos]) {
                    case '"':
                        this.Pos += 1;
                        break loop;

                    case '\\':
                        this.Pos += 2;
                        break;

                    default:
                        this.Pos += 1;
                        break;
                }
            }

            // 引用符は含まない。
            this.Word = this.Text.substr(st + 1, (this.Pos - 1) - (st + 1));
            this.WordType = $StringType;
            break;

        default:
            this.Pos++;
            loop2: for (; this.Pos < this.Text.length;) {
                switch (this.Text[this.Pos]) {
                    case '<':
                    case '>':
                    case '/':
                    case '?':
                    case '=':
                    case ':':
                    case '\'':
                    case '"':
                    case '\\':

                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        break loop2;

                    default:
                        this.Pos += 1;
                        break;
                }
            }

            this.Word = this.Text.substr(st, this.Pos - st);
            break;
    }

    return this.Word;
}

TXmlParser.prototype.GetToken = function (s) {
    var w = this.NextToken();
    if (w != s) {
        alert(w + " != " + s);
    }

    return w;
}

TXmlParser.prototype.GetString = function () {
    var w = this.NextToken();
    if (this.WordType != $StringType) {
        alert("Not string : " + w);
    }

    return w;
}

TXmlParser.prototype.GetFieldTypeSub = function (obj_proto, field_name) {
    var field_list = obj_proto.$FieldList;

    if (field_list != undefined) {
        var tp = field_list[field_name];

        if (tp != undefined) {

            return tp;
        }
    }

    if (obj_proto.SuperClass != undefined) {

        return this.GetFieldTypeSub(obj_proto.SuperClass, field_name);
    }

    return undefined;
}

TXmlParser.prototype.GetFieldType = function (obj_class, field_name) {
    return this.GetFieldTypeSub(obj_class.prototype, field_name);
}

TXmlParser.prototype.ReadAttribute = function () {
    var attr = new Object();

    attr.xmlns = new Array();


    // オブジェクトの属性を読む。
    for (; ;) {
        var s = this.NextToken();

        if (s == ">") {

            this.Content = true;
            break;
        }
        else if (s == "/>") {
            this.Content = false;
            break;
        }
        else if (s == "xmlns") {

            this.GetToken(":");
            var ns = this.NextToken();

            this.GetToken("=");

            attr.xmlns[ns] = this.GetString();

            $Log("name space : " + ns + " = " + attr.xmlns[ns]);
        }
        else if (s == "xsi") {

            this.GetToken(":");
            this.GetToken("type");

            this.GetToken("=");

            attr.TypeName = this.GetString();

            $Log("type name = " + attr.TypeName);
        }
        else {

            alert("不明のオブジェクト属性 : " + word);
            break;
        }
    }

    return attr;
}

TXmlParser.prototype.PrimitiveValue = function (val_class, str) {
    if (val_class == $IntType) {
        var n = parseInt(str, 10);

        $Log("    int : " + n);

        return n;
    }
    else if (val_class == $DoubleType) {
        var d = parseFloat(str);

        $Log("    double : " + d);

        return d;
    }
    else if (val_class == $StringType) {
        $Log("    string : " + str);

        return str;
    }
    else if (val_class == $BooleanType) {
        var b = (str == "true" ? true : (str == "false" ? false : undefined));
        $Log("    Boolean : " + b);

        return b;
    }
    else {
        //        var e = eval(val_class.prototype.$ClassName + "." + str);
        var e = val_class[str];

        $Log("    enum : " + str + " : " + e);

        return e;
    }
}

TXmlParser.prototype.ReadField = function (obj, obj_class, field_name) {
    var class_name = obj_class.prototype.$ClassName;

    var field_attr = this.ReadAttribute();

    if (!this.Content) {

        $Log("field : " + class_name + "." + field_name + " = no content");
        return undefined;
    }

    var field_type;
    if (field_attr.TypeName != undefined) {
        // フィールドの型が指定されている場合

        field_type = $GetClassByName(field_attr.TypeName);
    }
    else {
        // フィールドの型が指定されていない場合

        field_type = this.GetFieldType(obj_class, field_name);

        if (field_type == undefined) {
            // フィールドの型が不明の場合

            alert("フィールドの型が不明 : " + field_name);
            return undefined;
        }
    }
    var field_type_name = field_type.prototype.$ClassName;

    var field_obj;

    if (field_type == $ArrayType) {

        var array_obj = new Array();
        for (; ;) {
            var ele = this.ReadObject();
            if (ele == undefined) {

                Debug.Assert(this.Word == "</", "read field : array");
                this.GetToken(field_name);
                this.GetToken(">");
                break;
            }

            if (array_obj.ElementType == undefined) {

                array_obj.ElementType = ele.Type;
            }

            $Log("array : " + class_name + "." + field_name + " = " + ele.Value);

            array_obj.push(ele.Value);
        }

        return array_obj;
    }
    else if (field_type.prototype.$FieldList == undefined) {
        // プリミティブ型か列挙型の場合

        var end_tag = "</" + field_name + ">";
        var k = this.Text.indexOf(end_tag, this.Pos);
        Debug.Assert(k != -1, "read field : primitive");

        var str = this.Text.substr(this.Pos, k - this.Pos);
        this.Pos = k + end_tag.length;

        $Log("field : " + class_name + "." + field_name + "." + field_type_name + " = " + str);

        var val = this.PrimitiveValue(field_type, str);

        return val;
    }
    else {
        // オブジェクトの場合

        $Log("field obj : {0} {1}", field_type_name, field_type.prototype.$ClassName);


        field_obj = eval("new " + field_type.prototype.$ClassName + "()");

        $Log("field : " + class_name + "." + field_name + " = new " + field_type_name);

        // オブジェクトのフィールドを読む。
        this.ReadFieldList(field_obj, field_type, field_name);

        return field_obj;
    }
}

TXmlParser.prototype.ReadFieldList = function (obj, obj_class, end_tag) {
    for (; ;) {
        var s = this.NextToken();

        if (s == "</") {
            // オブジェクト定義の終わりの場合

            this.GetToken(end_tag);
            this.GetToken(">");
            break;
        }
        else if (s == "<") {
            // オブジェクトのフィールドの始まりの場合

            var field_name = this.NextToken();

            obj[field_name] = this.ReadField(obj, obj_class, field_name);
        }
        else {
            alert("Object Field syntax error : " + s);
            break;
        }
    }
}

TXmlParser.prototype.ReadObject = function () {
    var str = this.NextToken();


    if (str != "<") {
        return undefined;
    }

    var class_name = this.NextToken();
    var obj_class = $GetClassByName(class_name);

    var val;
    if (obj_class.prototype.$FieldList == undefined) {
        // プリミティブ型か列挙型の場合

        this.GetToken(">");

        var end_tag = "</" + class_name + ">";
        var k = this.Text.indexOf(end_tag, this.Pos);
        Debug.Assert(k != -1, "read object");

        var val_str = this.Text.substr(this.Pos, k - this.Pos);
        this.Pos = k + end_tag.length;

        val = this.PrimitiveValue(obj_class, val_str);
    }
    else {
        // オブジェクトの場合

        $Log("read obj : {0} {1}", class_name, obj_class.prototype.$ClassName);

        val = eval("new " + class_name + "()");
        $Log("new " + class_name);

        var attr = this.ReadAttribute();

        // オブジェクトのフィールドを読む。
        this.ReadFieldList(val, obj_class, class_name);
    }

    return { "Value": val, "Type": obj_class };
}

TXmlParser.prototype.ReadHeader = function () {
    var word = this.NextToken();

    if (word == "<?") {

        this.GetToken("xml");

        for (; ;) {

            word = this.NextToken();
            if (word == "version") {

                this.GetToken("=");
                this.version = this.GetString();

                $Log("バージョン : " + this.version);
            }
            else if (word == "encoding") {

                this.GetToken("=");
                this.encoding = this.GetString();

                $Log("文字コード : " + this.encoding);
            }
            else if (word == "?>") {
                break;
            }
            else {
                alert("不明のヘッダ属性 : " + word);
                break;
            }
        }
    }
}

TXmlParser.prototype.Parse = function () {
    this.ReadHeader();

    return this.ReadObject().Value;
}

//-------------------------------------------------------------------------------- TXmlSerializer
var TXmlSerializer = function () {
    this.Text = "";
}

TXmlSerializer.prototype.XmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";

TXmlSerializer.prototype.writeln = function () {
    var s = $Format.apply(null, arguments);
    this.Text += s + "\r\n";
}

TXmlSerializer.prototype.Serialize = function (tag_name, tag_is_field_name, obj_class, obj, tab) {
    var sp = $TabSpace(tab * 4);

    if (obj == undefined) {

//        this.writeln(sp + "<{0} />", tag_name);
    }
    else {

        if (Array.isArray(obj)) {
            // 配列の場合

            this.writeln(sp + "<{0}>", tag_name);
            for (var i = 0; i < obj.length; i++) {

                var ele = obj[i];
                var element_type_name;
                if (ele.$ClassName != undefined) {

                    element_type_name = ele.$ClassName;
                }
                else {

                    element_type_name = obj.ElementType.prototype.$ClassName;
                }
                this.Serialize(element_type_name, false, $GetClassByName(element_type_name), ele, tab + 1);
            }
            this.writeln(sp + "</{0}>", tag_name);
        }
        else if (typeof obj === "object" && obj !== null) {
            // オブジェクトの場合

            if (tag_is_field_name) {

                this.writeln(sp + "<{0} xsi:type=\"{1}\">", tag_name, obj.$ClassName);
            }
            else {

                this.writeln(sp + "<{0}>", tag_name);
            }
            this.SerializeFieldList($GetClassByName(obj.$ClassName), obj, tab + 1);
            this.writeln(sp + "</{0}>", tag_name);
        }
        else if ($IsSysClass(obj_class)) {
            // プリミティブ型の場合

            this.writeln(sp + "<{0}>{1}</{0}>", tag_name, obj);
        }
        else {
            // 列挙型の場合

            //$Log("keys A : {0}", obj_class);
            var keys = Object.keys(obj_class);
            for (var i = 0; i < keys.length; i++) {
                var field_name = keys[i];
                if (obj_class[field_name] == obj) {

                    this.writeln(sp + "<{0}>{1}</{0}>", tag_name, field_name);
                }
            }
        }
    }
}

TXmlSerializer.prototype.SerializeFieldListSub = function (obj_proto, obj, tab) {
    //if (obj_proto.$ClassName == undefined) {
    //    $Log("");
    //}
    //$Log("keys B : {0} {1}", obj_proto.$ClassName, obj_proto.$FieldList);
    var keys = Object.keys(obj_proto.$FieldList);
    for (var i = 0; i < keys.length; i++) {
        var field_name = keys[i];
        this.Serialize(field_name, true, obj_proto.$FieldList[field_name], obj[field_name], tab);
    }
    if (obj_proto.SuperClass != undefined) {
        this.SerializeFieldListSub(obj_proto.SuperClass, obj, tab);
    }
}

TXmlSerializer.prototype.SerializeFieldList = function (obj_class, obj, tab) {
    this.SerializeFieldListSub(obj_class.prototype, obj, tab);
}

TXmlSerializer.prototype.SerializeTop = function (obj) {
    this.writeln(this.XmlHeader);
    this.writeln("<{0} xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">", obj.$ClassName);

    this.SerializeFieldList($GetClassByName(obj.$ClassName), obj, 1);
    this.writeln("</{0}>", obj.$ClassName);
}
