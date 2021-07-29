using UnityEngine;



namespace CubeMarker
{
    public class ControllerManual : ControllerBase, IController
    {
        private float _smoothness = 0;
        public float smoothness { get{return _smoothness;} set{
            if (value > 0.9f) _smoothness = 0.9f;
            else if (value < 0f) _smoothness = 0;
            else _smoothness = value;
        }}
        private float tr = 0, ro = 0;

        protected virtual float GetKeyForward() {return 0;}
        protected virtual float GetKeyBackward() {return 0;}
        protected virtual float GetKeyLeft() {return 0;}
        protected virtual float GetKeyRight() {return 0;}

        protected override bool Run(Observation obs = null)
        {
            float w = GetKeyForward();
            float s = GetKeyBackward();
            float a = GetKeyLeft();
            float d = GetKeyRight();

            float tarTranslate = (w + s) * maxTranslate;
            float tarRotate = (a + d) * maxRotate;
            tr = tarTranslate + (tr - tarTranslate) * _smoothness;
            ro = tarRotate + (ro - tarRotate) * _smoothness;

            int newL = (int)(tr - ro * Mathf.Sign(tr));
            int newR = (int)(tr + ro * Mathf.Sign(tr));

            if (newL == uL && newR == uR) return false;
            uL = newL; uR = newR; return true;
        }
    }

    public class ConJoystick : ControllerManual
    {
        public FloatingJoystick joystick;

        protected override float GetKeyForward()
        { return Mathf.Clamp(joystick.Vertical, 0, 1); }
        protected override float GetKeyBackward()
        { return Mathf.Clamp(joystick.Vertical, -1, 0); }
        protected override float GetKeyLeft()
        { return Mathf.Clamp(-joystick.Horizontal, 0, 1); }
        protected override float GetKeyRight()
        { return Mathf.Clamp(-joystick.Horizontal, -1, 0); }

    }

    public class ConKeyboard : ControllerManual
    {
        protected override float GetKeyForward()
        {return (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))? 1:0;}
        protected override float GetKeyBackward()
        {return (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))? -1:0;}
        protected override float GetKeyLeft()
        {return (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))? 1:0;}
        protected override float GetKeyRight()
        {return (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))? -1:0;}
    }

    public class ConKeyboard_Joystick : ControllerManual
    {
        public FloatingJoystick joystick;

        protected override float GetKeyForward()
        {
            var j = Mathf.Clamp(joystick.Vertical, 0, 1);
            var k = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))? 1:0;
            return Mathf.Max(j, k);
        }
        protected override float GetKeyBackward()
        {
            var j = Mathf.Clamp(joystick.Vertical, -1, 0);
            var k = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))? -1:0;
            return Mathf.Min(j, k);
        }
        protected override float GetKeyLeft()
        {
            var j = Mathf.Clamp(-joystick.Horizontal, 0, 1);
            var k = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))? 1:0;
            return Mathf.Max(j, k);
        }
        protected override float GetKeyRight()
        {
            var j = Mathf.Clamp(-joystick.Horizontal, -1, 0);
            var k = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))? -1:0;
            return Mathf.Min(j, k);
        }

    }
}
