using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Whiplash.WeaponTracers
{
    public class WeaponTracer
    {
        public bool Remove = false;
        public Vector3D To;

        Vector3D _from;
        Vector3D _direction;
        Vector3D _velocity;
        Vector3 _tracerColor;
        Vector4 _trailColor;
        float _tracerScale;
        float _trailDecayMult;
        bool _drawFullTracer;
        bool _drawTrail;
        bool _hasDrawnTracer = false;
        bool _skip = true;
        const float TrailKillThreshold = 0.01f;
        const double Tick = 1.0 / 60.0;

        readonly MyStringId _materialTrail = MyStringId.GetOrCompute("WeaponLaser");
        readonly MyStringId _materialSquare = MyStringId.GetOrCompute("Square");
        readonly MyStringId _materialDot = MyStringId.GetOrCompute("WhiteDot");

        public WeaponTracer(Vector3D direction, Vector3D velocity, Vector3D lineFrom, Vector3D lineTo, Vector3 tracerColor, float tracerScale, float decayRatio, bool drawFullTracer, bool drawTrail)
        {
            _velocity = velocity;
            _from = lineFrom;
            To = lineTo;
            _tracerColor = tracerColor;
            _tracerScale = tracerScale;
            _trailDecayMult = MathHelper.Clamp(1f - decayRatio, 0f, 1f);
            _drawFullTracer = drawFullTracer;
            _drawTrail = drawTrail;
            _direction = direction;
            if (!Vector3D.IsUnit(ref _direction))
                _direction.Normalize();

            _trailColor = new Vector4(_tracerColor, 1f);
        }

        public void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
            {
                Remove = true;
                return;
            }

            // Update positions due to initial shooter velocity
            var step = _velocity * Tick;
            _from += step;

            if (_skip)
                _skip = false;
            else
                To += step;

            // Draw bullet trail
            if (_drawTrail)
            {
                MySimpleObjectDraw.DrawLine(_from, To, _materialTrail, ref _trailColor, _tracerScale * 0.1f);
                _trailColor *= _trailDecayMult;
                if (_trailColor.LengthSquared() < TrailKillThreshold)
                    Remove = true;
            }
            else if (_hasDrawnTracer)
            {
                Remove = true;
            }

            // Draw tracer
            if (!_hasDrawnTracer)
            {
                float lengthMultiplier = 0;
                Vector3D startPoint = Vector3D.Zero;

                if (_drawFullTracer)
                {
                    lengthMultiplier = 0.6f * 40f * _tracerScale;
                    startPoint = To - _direction * lengthMultiplier;
                }
                else
                {
                    startPoint = _from;
                    lengthMultiplier = (float)Vector3D.Distance(To, _from);
                }

                float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
                float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * _tracerScale;
                thickness *= 0.4f; //MathHelper.Lerp(0.2f, 0.8f, 1f);

                var colorVec = new Vector4(_tracerColor * scaleFactor * 10f, 1f);
                MyTransparentGeometry.AddLineBillboard(_materialSquare, colorVec, startPoint, (Vector3)_direction, lengthMultiplier, thickness);
                MyTransparentGeometry.AddPointBillboard(_materialDot, colorVec, startPoint, thickness, 0);
                MyTransparentGeometry.AddPointBillboard(_materialDot, colorVec, startPoint + _direction * lengthMultiplier, thickness, 0);
                _hasDrawnTracer = true;
            }
        }
    }
}
