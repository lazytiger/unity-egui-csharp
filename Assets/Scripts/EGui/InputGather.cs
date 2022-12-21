using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Proto;
using UnityEngine;
using Event = Proto.Event;
using EventType = Proto.EventType;
using Input = Proto.Input;
using Touch = Proto.Touch;
using TouchPhase = Proto.TouchPhase;
using UnityInput = UnityEngine.Input;

namespace EGui
{
    public class InputGather
    {
        private static readonly KeyCode[] UnityKeys =
        {
            KeyCode.A, KeyCode.B, KeyCode.Backspace, KeyCode.C, KeyCode.D, KeyCode.Delete, KeyCode.E, KeyCode.End,
            KeyCode.KeypadEnter, KeyCode.Escape, KeyCode.F, KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5,
            KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13,
            KeyCode.F14,
            KeyCode.F15,
            KeyCode.G, KeyCode.H, KeyCode.Home, KeyCode.I,
            KeyCode.Insert, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.Keypad0, KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9,
            KeyCode.O,
            KeyCode.P,
            KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.Space, KeyCode.T, KeyCode.Tab,
            KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
            KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.PageDown, KeyCode.PageUp,
        };

        private static readonly KeyType[] EguiKeys =
        {
            KeyType.A, KeyType.B, KeyType.Backspace, KeyType.C, KeyType.D, KeyType.Delete, KeyType.E, KeyType.End,
            KeyType.Enter, KeyType.Escape, KeyType.F, KeyType.F1, KeyType.F2, KeyType.F3, KeyType.F4, KeyType.F5,
            KeyType.F6, KeyType.F7, KeyType.F8, KeyType.F9, KeyType.F10, KeyType.F11, KeyType.F12, KeyType.F13,
            KeyType.F14,
            KeyType.F15,
            KeyType.G, KeyType.H, KeyType.Home, KeyType.I,
            KeyType.Insert, KeyType.J, KeyType.K, KeyType.L, KeyType.M, KeyType.N, KeyType.Num0, KeyType.Num1,
            KeyType.Num2,
            KeyType.Num3,
            KeyType.Num4, KeyType.Num5, KeyType.Num6, KeyType.Num7, KeyType.Num8, KeyType.Num9, KeyType.O, KeyType.P,
            KeyType.Q, KeyType.R, KeyType.S, KeyType.Space, KeyType.T, KeyType.Tab,
            KeyType.U, KeyType.V, KeyType.W, KeyType.X, KeyType.Y, KeyType.Z, KeyType.ArrowDown, KeyType.ArrowLeft,
            KeyType.ArrowRight, KeyType.ArrowUp, KeyType.PageDown, KeyType.PageUp
        };

        private static InputGather _instance;

        public static InputGather Instance
        {
            get { return _instance ??= new InputGather(); }
        }

        private readonly Input _input = new Input();

        readonly StringBuilder _sb = new StringBuilder();

        private Vector3 _mousePosition;

        private TouchScreenKeyboard _keyboard;

        private int _lastTouchId = -1;

        private string _lastTextInput = "";

        public void OpenKeyboard(int show, string current)
        {
#if !UNITY_EDITOR
            var status = show != 0;
            
            if (_keyboard == null && status)
            {
                Debug.Log("open keyboard");
                _lastTextInput = current;
                _keyboard = TouchScreenKeyboard.Open(current);
            }
            
            if (!status && _keyboard != null)
            {
                Debug.Log("close keyboard");
                _keyboard.active = false;
                _keyboard = null;
                _lastTextInput = "";
            }
#endif
        }

        private string GetKeyboardInput()
        {
            if (_keyboard != null)
            {
                switch (_keyboard.status)
                {
                    case TouchScreenKeyboard.Status.Visible:
                        break;
                    case TouchScreenKeyboard.Status.Done:
                        goto case default;
                    case TouchScreenKeyboard.Status.Canceled:
                        goto case default;
                    case TouchScreenKeyboard.Status.LostFocus:
                        goto case default;
                    default:
                        _keyboard = null;
                        break;
                }
            }

            return _keyboard != null ? _keyboard.text : "";
        }

        public Input GetInput()
        {
            _input.Events.Clear();
            _input.ScreenRect = new Proto.Rect
            {
                Min = new Pos2
                {
                    X = 0,
                    Y = 0,
                },
                Max = new Pos2
                {
                    X = Screen.width,
                    Y = Screen.height,
                }
            };
            _input.MaxTextureSide = (uint)SystemInfo.maxTextureSize;
            _input.Time = Time.time;
            _input.HasFocus = UnityInput.anyKey;
            _input.PixelsPerPoint = 1;
            if (Application.targetFrameRate > 0)
            {
                _input.PredictedDt = 1.0f / Application.targetFrameRate;
            }

            _input.Modifier = new Modifiers
            {
                Alt = UnityInput.GetKey(KeyCode.LeftAlt) || UnityInput.GetKey(KeyCode.RightAlt),
                Command = UnityInput.GetKey(KeyCode.LeftCommand) || UnityInput.GetKey(KeyCode.RightCommand),
                Ctrl = UnityInput.GetKey(KeyCode.LeftControl) || UnityInput.GetKey(KeyCode.RightControl),
                Shift = UnityInput.GetKey(KeyCode.LeftShift) || UnityInput.GetKey(KeyCode.RightShift),
                MacCmd = UnityInput.GetKey(KeyCode.LeftApple) || UnityInput.GetKey(KeyCode.RightApple)
            };
            if (UnityInput.anyKey)
            {
                for (var i = 0; i < UnityKeys.Length; i++)
                {
                    if (!UnityInput.GetKeyDown(UnityKeys[i])) continue;
                    var e = new Event
                    {
                        Et = EventType.Key,
                        Key = new Key
                        {
                            Key_ = EguiKeys[i],
                            Pressed = true
                        }
                    };
                    _input.Events.Add(e);
                }

                for (var mouse = 0; mouse < 3; mouse++)
                {
                    if (!UnityInput.GetMouseButtonDown(mouse)) continue;
                    var e = new Event
                    {
                        Et = EventType.PointerButton,
                        PointerButton = new PointerButton
                        {
                            Pos = Pos2FromVector2(UnityInput.mousePosition),
                            Button = EguiButtonTypeFromUnity(mouse),
                            Pressed = true
                        }
                    };
                    _input.Events.Add(e);
                }
            }
            else
            {
                for (var i = 0; i < UnityKeys.Length; i++)
                {
                    if (!UnityInput.GetKeyUp(UnityKeys[i])) continue;
                    var e = new Event
                    {
                        Et = EventType.Key,
                        Key = new Key
                        {
                            Key_ = EguiKeys[i],
                            Pressed = false
                        }
                    };
                    _input.Events.Add(e);
                }

                for (var mouse = 0; mouse < 3; mouse++)
                {
                    if (!UnityInput.GetMouseButtonUp(mouse)) continue;
                    var e = new Event
                    {
                        Et = EventType.PointerButton,
                        PointerButton = new PointerButton
                        {
                            Pos = Pos2FromVector2(UnityInput.mousePosition),
                            Button = EguiButtonTypeFromUnity(mouse),
                            Pressed = false
                        }
                    };
                    _input.Events.Add(e);
                }
            }

            for (var i = 0; i < UnityInput.touchCount; i++)
            {
                var touch = UnityInput.GetTouch(i);
                var e = new Event
                {
                    Et = EventType.Touch,
                    Touch = new Touch
                    {
                        Force = touch.pressure,
                        Phase = EguiPhaseFromUnity(touch.phase),
                        Pos = Pos2FromVector2(touch.position),
                        Id = (ulong)touch.fingerId,
                        DeviceId = 0
                    }
                };
                _input.Events.Add(e);

                if (_lastTouchId != touch.fingerId)
                {
                    continue;
                }

                switch (touch.phase)
                {
                    case UnityEngine.TouchPhase.Began:
                        _lastTouchId = touch.fingerId;
                        e = new Event
                        {
                            Et = EventType.PointerMoved,
                            PointerMoved = Pos2FromVector2(touch.position),
                        };
                        _input.Events.Add(e);
                        e = new Event
                        {
                            Et = EventType.PointerButton,
                            PointerButton = new PointerButton
                            {
                                Button = ButtonType.Primary,
                                Pressed = true,
                                Pos = Pos2FromVector2(touch.position),
                            }
                        };
                        _input.Events.Add(e);
                        break;
                    case UnityEngine.TouchPhase.Moved:
                        e = new Event
                        {
                            Et = EventType.PointerMoved,
                            PointerMoved = Pos2FromVector2(touch.position),
                        };
                        _input.Events.Add(e);
                        break;
                    case UnityEngine.TouchPhase.Stationary:
                        break;
                    case UnityEngine.TouchPhase.Ended:
                        _lastTouchId = -1;
                        e = new Event
                        {
                            Et = EventType.PointerButton,
                            PointerButton = new PointerButton
                            {
                                Button = ButtonType.Primary,
                                Pressed = false,
                                Pos = Pos2FromVector2(touch.position),
                            }
                        };
                        _input.Events.Add(e);
                        goto case UnityEngine.TouchPhase.Canceled;
                    case UnityEngine.TouchPhase.Canceled:
                        _lastTouchId = -1;
                        e = new Event
                        {
                            Et = EventType.PointerGone,
                        };
                        _input.Events.Add(e);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


#if UNITY_EDITOR
            var inputString = UnityInput.inputString;
            if (inputString.Length > 0)
            {
                Debug.Log($"input length:{inputString.Length}");
                _sb.Clear();
                foreach (var c in inputString)
                {
                    if (c != '\b')
                    {
                        _sb.Append(c);
                        if (c == '\r')
                        {
                            _sb.Append('\n');
                        }
                    }
                }

                inputString = _sb.ToString();
                Debug.Log($"input length:{inputString.Length}");
            }

#else
            var inputString = GetKeyboardInput();
            for (var i = 0; i < _lastTextInput.Length; i++)
            {
                var e = new Event
                {
                    Et = EventType.Key,
                    Key = new Key
                    {
                        Key_ = KeyType.Backspace,
                        Pressed = true,
                    }
                };
                _input.Events.Add(e);
                e = new Event
                {
                    Et = EventType.Key,
                    Key = new Key
                    {
                        Key_ = KeyType.Backspace,
                        Pressed = false,
                    }
                };
                _input.Events.Add(e);
            }
            _lastTextInput = inputString;
#endif
            if (inputString.Length > 0)
            {
                var e = new Event
                {
                    Et = EventType.Text,
                    Text = inputString,
                };
                _input.Events.Add(e);
            }

            if (!_mousePosition.Equals(UnityInput.mousePosition))
            {
                if (UnityInput.mousePosition.x < 0 || UnityInput.mousePosition.y < 0 ||
                    UnityInput.mousePosition.x > Screen.width || UnityInput.mousePosition.y > Screen.height)
                {
                    var e = new Event
                    {
                        Et = EventType.PointerGone
                    };
                    _input.Events.Add(e);
                }
                else
                {
                    var e = new Event
                    {
                        Et = EventType.PointerMoved,
                        PointerMoved = Pos2FromVector2(UnityInput.mousePosition)
                    };
                    _input.Events.Add(e);
                }

                _mousePosition = UnityInput.mousePosition;
            }

            var y = UnityInput.GetAxis("Mouse ScrollWheel");
            if (y != 0)
            {
                var e = new Event
                {
                    Et = EventType.Scroll,
                    Scroll = new Pos2
                    {
                        X = 0,
                        Y = y * 30,
                    }
                };
                _input.Events.Add(e);
            }

            return _input;
        }

        private static ButtonType EguiButtonTypeFromUnity(int mouse)
        {
            return mouse switch
            {
                0 => ButtonType.Primary,
                1 => ButtonType.Secondary,
                2 => ButtonType.Middle,
                _ => ButtonType.BtNone
            };
        }

        private static TouchPhase EguiPhaseFromUnity(UnityEngine.TouchPhase touchPhase)
        {
            return touchPhase switch
            {
                UnityEngine.TouchPhase.Began => TouchPhase.Start,
                UnityEngine.TouchPhase.Canceled => TouchPhase.Cancel,
                UnityEngine.TouchPhase.Ended => TouchPhase.End,
                UnityEngine.TouchPhase.Moved => TouchPhase.Move,
                UnityEngine.TouchPhase.Stationary => TouchPhase.TpNone,
                _ => TouchPhase.TpNone
            };
        }

        private static Pos2 Pos2FromVector2(Vector2 touchPosition)
        {
            var pos = new Pos2
            {
                X = touchPosition.x,
                Y = Screen.height - touchPosition.y
            };
            return pos;
        }
    }
}