
window.onload = function () {
    var runtime = new TRuntime();
    runtime.App = new TMyApplication();
    runtime.Canvas = document.getElementById("canvassample");
    runtime.RuntimeInitialize();
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

    this.App.AppInitialize();
    if (this.App.__SetParent) {
        this.App.__SetParent(this.App, undefined);
    }

    this.Run();
};

TRuntime.prototype.AnimationFrameLoop = function () {
    var _this = this;
    this.Graphics.Clear();
    this.App.Graphics = this.Graphics;
    this.App.Navigate_Rule(this.App, this.App);

    if (this.App.ShapeList != null) {
        this.Graphics.Context.setTransform(1, 0, 0, 1, 0, 0);
        for (var i = 0; i < this.App.ShapeList.length; i++) {
            this.App.ShapeList[i].Draw(this.Graphics);
        }
    }

    if (this.App.ViewList != null) {
        this.Graphics.Context.setTransform(1, 0, 0, 1, 0, 0);
        for (var i = 0; i < this.App.ViewList.length; i++) {
            this.App.ViewList[i].Draw(this.Graphics);
        }
    }

    window.requestAnimationFrame(function () { return _this.AnimationFrameLoop(); });
};

TRuntime.prototype.Run = function () {
    this.AnimationFrameLoop();
};

function Inherits(sub_class, super_class) {

    for (var p in super_class) {
        if (super_class.hasOwnProperty(p)) {
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
