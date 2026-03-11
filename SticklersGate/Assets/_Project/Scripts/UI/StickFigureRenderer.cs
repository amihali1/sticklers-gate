using UnityEngine;
using SticklersGate.Combat;

namespace SticklersGate.UI
{
    /// <summary>
    /// Renders a programmatic stick figure using LineRenderers and a circle for the head.
    /// Supports idle, attack, hit, and death poses. No sprite assets needed.
    /// </summary>
    public class StickFigureRenderer : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _bodyColor = Color.white;
        [SerializeField] private float _lineWidth = 0.05f;

        [Header("Scale")]
        [SerializeField] private float _height = 2f;
        [SerializeField] private bool _facingRight = true;

        private LineRenderer _bodyLine;
        private LineRenderer _leftArmLine;
        private LineRenderer _rightArmLine;
        private LineRenderer _leftLegLine;
        private LineRenderer _rightLegLine;
        private LineRenderer _headCircle;

        private StickPose _currentPose = StickPose.Idle;

        private static Material _sharedLineMaterial;

        /// <summary>
        /// Poses available for the stick figure.
        /// </summary>
        public enum StickPose
        {
            Idle,
            Attack,
            Hit,
            Death,
            Defend,
            Cast
        }

        /// <summary>
        /// Bind this renderer to a CombatUnit and set initial pose.
        /// </summary>
        public void Bind(CombatUnit unit, bool facingRight, float height = 2f)
        {
            _facingRight = facingRight;
            _height = height;

            // Determine color by class or enemy type
            if (unit.IsPlayer)
            {
                _bodyColor = GetPlayerColor(unit);
            }
            else
            {
                _bodyColor = GetEnemyColor(unit);
            }

            CreateLineRenderers();
            SetPose(StickPose.Idle);
        }

        /// <summary>
        /// Change the stick figure's pose.
        /// </summary>
        public void SetPose(StickPose pose)
        {
            _currentPose = pose;
            UpdatePose();
        }

        /// <summary>
        /// Show the death pose and fade out.
        /// </summary>
        public void ShowDeath()
        {
            SetPose(StickPose.Death);
            SetAlpha(0.3f);
        }

        private void CreateLineRenderers()
        {
            // Clear existing
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            _bodyLine = CreateLine("Body");
            _leftArmLine = CreateLine("LeftArm");
            _rightArmLine = CreateLine("RightArm");
            _leftLegLine = CreateLine("LeftLeg");
            _rightLegLine = CreateLine("RightLeg");
            _headCircle = CreateLine("Head");
        }

        private LineRenderer CreateLine(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();

            if (_sharedLineMaterial == null)
            {
                _sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            lr.sharedMaterial = _sharedLineMaterial;

            lr.startColor = _bodyColor;
            lr.endColor = _bodyColor;
            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth;
            lr.useWorldSpace = false;
            lr.sortingOrder = 1;

            return lr;
        }

        private void UpdatePose()
        {
            float dir = _facingRight ? 1f : -1f;
            float scale = _height / 2f;

            switch (_currentPose)
            {
                case StickPose.Idle:
                    SetIdlePose(dir, scale);
                    break;
                case StickPose.Attack:
                    SetAttackPose(dir, scale);
                    break;
                case StickPose.Hit:
                    SetHitPose(dir, scale);
                    break;
                case StickPose.Death:
                    SetDeathPose(dir, scale);
                    break;
                case StickPose.Defend:
                    SetDefendPose(dir, scale);
                    break;
                case StickPose.Cast:
                    SetCastPose(dir, scale);
                    break;
            }

            DrawHead(dir, scale);
        }

        private void SetIdlePose(float dir, float scale)
        {
            // Body: vertical line from hip to shoulder
            SetLinePositions(_bodyLine, new Vector3[]
            {
                new Vector3(0, 0, 0),           // Hip
                new Vector3(0, scale, 0)         // Shoulder
            });

            // Arms: from shoulder, angled down slightly
            SetLinePositions(_leftArmLine, new Vector3[]
            {
                new Vector3(0, scale, 0),
                new Vector3(-0.3f * dir * scale, scale * 0.7f, 0)
            });
            SetLinePositions(_rightArmLine, new Vector3[]
            {
                new Vector3(0, scale, 0),
                new Vector3(0.3f * dir * scale, scale * 0.7f, 0)
            });

            // Legs: from hip, angled out
            SetLinePositions(_leftLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-0.25f * scale, -scale, 0)
            });
            SetLinePositions(_rightLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.25f * scale, -scale, 0)
            });
        }

        private void SetAttackPose(float dir, float scale)
        {
            // Lean forward, arms extended
            SetLinePositions(_bodyLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.15f * dir * scale, scale, 0)
            });

            // Front arm extended forward
            SetLinePositions(_rightArmLine, new Vector3[]
            {
                new Vector3(0.15f * dir * scale, scale, 0),
                new Vector3(0.7f * dir * scale, scale * 0.9f, 0)
            });
            // Back arm back
            SetLinePositions(_leftArmLine, new Vector3[]
            {
                new Vector3(0.15f * dir * scale, scale, 0),
                new Vector3(-0.3f * dir * scale, scale * 0.6f, 0)
            });

            // Legs in lunge
            SetLinePositions(_leftLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-0.4f * dir * scale, -scale, 0)
            });
            SetLinePositions(_rightLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.3f * dir * scale, -scale, 0)
            });
        }

        private void SetHitPose(float dir, float scale)
        {
            // Lean backward
            SetLinePositions(_bodyLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-0.2f * dir * scale, scale * 0.9f, 0)
            });

            SetLinePositions(_leftArmLine, new Vector3[]
            {
                new Vector3(-0.2f * dir * scale, scale * 0.9f, 0),
                new Vector3(-0.5f * dir * scale, scale * 1.1f, 0)
            });
            SetLinePositions(_rightArmLine, new Vector3[]
            {
                new Vector3(-0.2f * dir * scale, scale * 0.9f, 0),
                new Vector3(0.1f * dir * scale, scale * 1.2f, 0)
            });

            SetLinePositions(_leftLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-0.2f * scale, -scale, 0)
            });
            SetLinePositions(_rightLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.3f * scale, -scale * 0.8f, 0)
            });
        }

        private void SetDeathPose(float dir, float scale)
        {
            // Lying flat on the ground (rotated 90 degrees)
            SetLinePositions(_bodyLine, new Vector3[]
            {
                new Vector3(-scale * 0.5f * dir, -scale * 0.8f, 0),
                new Vector3(scale * 0.5f * dir, -scale * 0.8f, 0)
            });

            SetLinePositions(_leftArmLine, new Vector3[]
            {
                new Vector3(scale * 0.5f * dir, -scale * 0.8f, 0),
                new Vector3(scale * 0.7f * dir, -scale * 0.5f, 0)
            });
            SetLinePositions(_rightArmLine, new Vector3[]
            {
                new Vector3(scale * 0.5f * dir, -scale * 0.8f, 0),
                new Vector3(scale * 0.3f * dir, -scale * 0.5f, 0)
            });

            SetLinePositions(_leftLegLine, new Vector3[]
            {
                new Vector3(-scale * 0.5f * dir, -scale * 0.8f, 0),
                new Vector3(-scale * 0.8f * dir, -scale * 0.6f, 0)
            });
            SetLinePositions(_rightLegLine, new Vector3[]
            {
                new Vector3(-scale * 0.5f * dir, -scale * 0.8f, 0),
                new Vector3(-scale * 0.7f * dir, -scale * 1.0f, 0)
            });
        }

        private void SetDefendPose(float dir, float scale)
        {
            // Crouched, arms up in guard
            SetLinePositions(_bodyLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, scale * 0.8f, 0)
            });

            SetLinePositions(_leftArmLine, new Vector3[]
            {
                new Vector3(0, scale * 0.8f, 0),
                new Vector3(-0.3f * dir * scale, scale * 1.0f, 0)
            });
            SetLinePositions(_rightArmLine, new Vector3[]
            {
                new Vector3(0, scale * 0.8f, 0),
                new Vector3(0.3f * dir * scale, scale * 1.0f, 0)
            });

            SetLinePositions(_leftLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-0.3f * scale, -scale * 0.7f, 0)
            });
            SetLinePositions(_rightLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.3f * scale, -scale * 0.7f, 0)
            });
        }

        private void SetCastPose(float dir, float scale)
        {
            // Arms raised overhead for casting
            SetLinePositions(_bodyLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, scale, 0)
            });

            SetLinePositions(_leftArmLine, new Vector3[]
            {
                new Vector3(0, scale, 0),
                new Vector3(-0.4f * dir * scale, scale * 1.5f, 0)
            });
            SetLinePositions(_rightArmLine, new Vector3[]
            {
                new Vector3(0, scale, 0),
                new Vector3(0.4f * dir * scale, scale * 1.5f, 0)
            });

            SetLinePositions(_leftLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-0.2f * scale, -scale, 0)
            });
            SetLinePositions(_rightLegLine, new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0.2f * scale, -scale, 0)
            });
        }

        private void DrawHead(float dir, float scale)
        {
            if (_headCircle == null) return;

            // Head position is above the shoulder
            Vector3 shoulderPos = _bodyLine.GetPosition(1);
            float headRadius = scale * 0.2f;
            Vector3 headCenter = shoulderPos + new Vector3(0, headRadius + 0.02f, 0);

            // Approximate circle with line segments
            int segments = 16;
            _headCircle.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = headCenter.x + Mathf.Cos(angle) * headRadius;
                float y = headCenter.y + Mathf.Sin(angle) * headRadius;
                _headCircle.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private void SetLinePositions(LineRenderer lr, Vector3[] positions)
        {
            if (lr == null) return;
            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
        }

        private void SetAlpha(float alpha)
        {
            SetLineAlpha(_bodyLine, alpha);
            SetLineAlpha(_leftArmLine, alpha);
            SetLineAlpha(_rightArmLine, alpha);
            SetLineAlpha(_leftLegLine, alpha);
            SetLineAlpha(_rightLegLine, alpha);
            SetLineAlpha(_headCircle, alpha);
        }

        private void SetLineAlpha(LineRenderer lr, float alpha)
        {
            if (lr == null) return;
            var color = lr.startColor;
            color.a = alpha;
            lr.startColor = color;
            lr.endColor = color;
        }

        private static Color GetPlayerColor(CombatUnit unit)
        {
            if (unit.ClassData == null) return Color.white;

            switch (unit.ClassData.ClassType)
            {
                case Data.CharacterClassType.Sticknight:
                    return new Color(0.6f, 0.6f, 0.9f);  // Steel blue
                case Data.CharacterClassType.Stickwizard:
                    return new Color(0.7f, 0.3f, 0.9f);  // Purple
                case Data.CharacterClassType.Stickrogue:
                    return new Color(0.3f, 0.7f, 0.3f);  // Green
                case Data.CharacterClassType.Stickeric:
                    return new Color(0.9f, 0.8f, 0.3f);  // Gold
                default:
                    return Color.white;
            }
        }

        private static Color GetEnemyColor(CombatUnit unit)
        {
            if (unit.EnemyData == null) return new Color(0.8f, 0.3f, 0.3f);

            switch (unit.EnemyData.Tier)
            {
                case Data.EnemyTier.Minion:
                    return new Color(0.7f, 0.4f, 0.4f);
                case Data.EnemyTier.Standard:
                    return new Color(0.8f, 0.3f, 0.3f);
                case Data.EnemyTier.Elite:
                    return new Color(0.9f, 0.2f, 0.5f);
                case Data.EnemyTier.Boss:
                    return new Color(0.6f, 0.1f, 0.1f);
                default:
                    return new Color(0.8f, 0.3f, 0.3f);
            }
        }
    }
}
