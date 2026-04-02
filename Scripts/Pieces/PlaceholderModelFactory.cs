using UnityEngine;
using WizardChess.Core;
using WizardChess.Visual;

namespace WizardChess.Pieces
{
    /// <summary>
    /// Static factory that creates placeholder 3D models using Unity primitives
    /// for each chess piece type. Each type has a visually distinct composite shape.
    /// Models include appropriate colliders for raycasting and colored materials.
    /// </summary>
    public static class PlaceholderModelFactory
    {

        /// <summary>
        /// Creates a placeholder 3D model for the given piece type and color.
        /// Returns a GameObject with composite primitives, materials, and a BoxCollider for raycasting.
        /// </summary>
        public static GameObject CreatePieceModel(PieceType type, PieceColor color, float tileSize = 1.0f)
        {
            GameObject root = new GameObject($"Piece_{color}_{type}");

            switch (type)
            {
                case PieceType.Pawn:
                    BuildPawn(root, tileSize);
                    break;
                case PieceType.Rook:
                    BuildRook(root, tileSize);
                    break;
                case PieceType.Knight:
                    BuildKnight(root, tileSize);
                    break;
                case PieceType.Bishop:
                    BuildBishop(root, tileSize);
                    break;
                case PieceType.Queen:
                    BuildQueen(root, tileSize);
                    break;
                case PieceType.King:
                    BuildKing(root, tileSize);
                    break;
            }

            ApplyColor(root, color);
            AddRaycastCollider(root, type, tileSize);

            return root;
        }

        /// <summary>
        /// Pawn: small armored soldier — capsule body + small sphere head.
        /// </summary>
        private static void BuildPawn(GameObject root, float ts)
        {
            // Body — small capsule
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.25f, ts * 0.3f, ts * 0.25f);
            body.transform.localPosition = new Vector3(0f, ts * 0.3f, 0f);

            // Head — small sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.18f;
            head.transform.localPosition = new Vector3(0f, ts * 0.62f, 0f);

            // Spear — thin cylinder
            var spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spear.name = "Spear";
            spear.transform.SetParent(root.transform, false);
            spear.transform.localScale = new Vector3(ts * 0.04f, ts * 0.35f, ts * 0.04f);
            spear.transform.localPosition = new Vector3(ts * 0.12f, ts * 0.45f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Rook: heavy stone golem — cube body + smaller cube head + cube shoulders.
        /// </summary>
        private static void BuildRook(GameObject root, float ts)
        {
            // Base — wide cube
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.4f, ts * 0.15f, ts * 0.4f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.075f, 0f);

            // Body — tall cube
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.35f, ts * 0.4f, ts * 0.35f);
            body.transform.localPosition = new Vector3(0f, ts * 0.35f, 0f);

            // Battlements — small cubes on top (like a rook tower)
            float bSize = ts * 0.12f;
            float bY = ts * 0.6f;
            float bOffset = ts * 0.12f;
            Vector3[] bPositions = {
                new Vector3(-bOffset, bY, -bOffset),
                new Vector3(bOffset, bY, -bOffset),
                new Vector3(-bOffset, bY, bOffset),
                new Vector3(bOffset, bY, bOffset)
            };
            for (int i = 0; i < bPositions.Length; i++)
            {
                var battlement = GameObject.CreatePrimitive(PrimitiveType.Cube);
                battlement.name = $"Battlement_{i}";
                battlement.transform.SetParent(root.transform, false);
                battlement.transform.localScale = Vector3.one * bSize;
                battlement.transform.localPosition = bPositions[i];
            }

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Knight: mounted warrior — capsule body tilted forward + sphere head + angled neck.
        /// </summary>
        private static void BuildKnight(GameObject root, float ts)
        {
            // Body — capsule
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.28f, ts * 0.35f, ts * 0.28f);
            body.transform.localPosition = new Vector3(0f, ts * 0.35f, 0f);

            // Neck — cylinder tilted forward (horse neck)
            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform, false);
            neck.transform.localScale = new Vector3(ts * 0.12f, ts * 0.2f, ts * 0.12f);
            neck.transform.localPosition = new Vector3(0f, ts * 0.7f, ts * 0.08f);
            neck.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Head — sphere (horse head)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.2f;
            head.transform.localPosition = new Vector3(0f, ts * 0.82f, ts * 0.18f);

            // Ears — two small cubes
            var earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earL.name = "EarL";
            earL.transform.SetParent(root.transform, false);
            earL.transform.localScale = new Vector3(ts * 0.04f, ts * 0.1f, ts * 0.04f);
            earL.transform.localPosition = new Vector3(-ts * 0.06f, ts * 0.95f, ts * 0.18f);

            var earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earR.name = "EarR";
            earR.transform.SetParent(root.transform, false);
            earR.transform.localScale = new Vector3(ts * 0.04f, ts * 0.1f, ts * 0.04f);
            earR.transform.localPosition = new Vector3(ts * 0.06f, ts * 0.95f, ts * 0.18f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Bishop: battle mage with staff — tall thin cylinder body + sphere head + thin staff.
        /// </summary>
        private static void BuildBishop(GameObject root, float ts)
        {
            // Base — cylinder
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.3f, ts * 0.08f, ts * 0.3f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.08f, 0f);

            // Body — tall thin cylinder
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.18f, ts * 0.35f, ts * 0.18f);
            body.transform.localPosition = new Vector3(0f, ts * 0.43f, 0f);

            // Head — sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.16f;
            head.transform.localPosition = new Vector3(0f, ts * 0.78f, 0f);

            // Mitre tip — small sphere on top (bishop hat)
            var mitre = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mitre.name = "Mitre";
            mitre.transform.SetParent(root.transform, false);
            mitre.transform.localScale = Vector3.one * ts * 0.08f;
            mitre.transform.localPosition = new Vector3(0f, ts * 0.9f, 0f);

            // Staff — thin cylinder to the side
            var staff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            staff.name = "Staff";
            staff.transform.SetParent(root.transform, false);
            staff.transform.localScale = new Vector3(ts * 0.03f, ts * 0.45f, ts * 0.03f);
            staff.transform.localPosition = new Vector3(ts * 0.15f, ts * 0.45f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Queen: elite warrior-mage — sphere body (larger) + crown ring + orb on top.
        /// </summary>
        private static void BuildQueen(GameObject root, float ts)
        {
            // Base — cylinder
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.35f, ts * 0.08f, ts * 0.35f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.08f, 0f);

            // Body — large sphere
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.35f, ts * 0.4f, ts * 0.35f);
            body.transform.localPosition = new Vector3(0f, ts * 0.4f, 0f);

            // Neck — thin cylinder
            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform, false);
            neck.transform.localScale = new Vector3(ts * 0.12f, ts * 0.12f, ts * 0.12f);
            neck.transform.localPosition = new Vector3(0f, ts * 0.65f, 0f);

            // Crown — flattened cylinder (ring)
            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Crown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localScale = new Vector3(ts * 0.22f, ts * 0.04f, ts * 0.22f);
            crown.transform.localPosition = new Vector3(0f, ts * 0.8f, 0f);

            // Orb — small sphere on top
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb";
            orb.transform.SetParent(root.transform, false);
            orb.transform.localScale = Vector3.one * ts * 0.1f;
            orb.transform.localPosition = new Vector3(0f, ts * 0.88f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// King: armored king with crown and sword — tallest cylinder body + cross on top + sword.
        /// </summary>
        private static void BuildKing(GameObject root, float ts)
        {
            // Base — cylinder
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.35f, ts * 0.1f, ts * 0.35f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.1f, 0f);

            // Body — tallest cylinder
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.22f, ts * 0.4f, ts * 0.22f);
            body.transform.localPosition = new Vector3(0f, ts * 0.5f, 0f);

            // Head — sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.18f;
            head.transform.localPosition = new Vector3(0f, ts * 0.9f, 0f);

            // Crown — flattened cylinder
            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Crown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localScale = new Vector3(ts * 0.24f, ts * 0.04f, ts * 0.24f);
            crown.transform.localPosition = new Vector3(0f, ts * 1.0f, 0f);

            // Cross vertical — thin cube on top of crown
            var crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossV.name = "CrossVertical";
            crossV.transform.SetParent(root.transform, false);
            crossV.transform.localScale = new Vector3(ts * 0.04f, ts * 0.14f, ts * 0.04f);
            crossV.transform.localPosition = new Vector3(0f, ts * 1.12f, 0f);

            // Cross horizontal — thin cube
            var crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossH.name = "CrossHorizontal";
            crossH.transform.SetParent(root.transform, false);
            crossH.transform.localScale = new Vector3(ts * 0.1f, ts * 0.04f, ts * 0.04f);
            crossH.transform.localPosition = new Vector3(0f, ts * 1.14f, 0f);

            // Sword — thin cylinder to the side
            var sword = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sword.name = "Sword";
            sword.transform.SetParent(root.transform, false);
            sword.transform.localScale = new Vector3(ts * 0.03f, ts * 0.4f, ts * 0.03f);
            sword.transform.localPosition = new Vector3(ts * 0.18f, ts * 0.5f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Applies the appropriate color scheme to all renderers in the piece hierarchy
        /// using MaterialFactory for consistent, high-quality materials.
        /// White: warm light stone base + gold accents + blue glow.
        /// Black: dark stone base + metallic accents + red-purple glow.
        /// </summary>
        private static void ApplyColor(GameObject root, PieceColor color)
        {
            Material baseMat = color == PieceColor.White
                ? MaterialFactory.CreateWhitePieceBaseMaterial()
                : MaterialFactory.CreateBlackPieceBaseMaterial();

            Material accentMat = color == PieceColor.White
                ? MaterialFactory.CreateWhitePieceAccentMaterial()
                : MaterialFactory.CreateBlackPieceAccentMaterial();

            Material weaponMat = color == PieceColor.White
                ? MaterialFactory.CreateWhitePieceWeaponMaterial()
                : MaterialFactory.CreateBlackPieceWeaponMaterial();

            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                string partName = r.gameObject.name;

                if (partName == "Crown" || partName == "CrossVertical" || partName == "CrossHorizontal"
                    || partName == "Mitre" || partName == "Orb")
                {
                    r.material = Object.Instantiate(accentMat);
                }
                else if (partName == "Staff" || partName == "Sword" || partName == "Spear")
                {
                    r.material = Object.Instantiate(weaponMat);
                }
                else
                {
                    r.material = Object.Instantiate(baseMat);
                }
            }

            // Clean up template materials
            Object.Destroy(baseMat);
            Object.Destroy(accentMat);
            Object.Destroy(weaponMat);
        }

        /// <summary>
        /// Adds a single BoxCollider to the root object encompassing the piece for raycasting.
        /// </summary>
        private static void AddRaycastCollider(GameObject root, PieceType type, float ts)
        {
            var collider = root.AddComponent<BoxCollider>();

            // Size the collider to roughly encompass the piece
            float height;
            float width;
            switch (type)
            {
                case PieceType.Pawn:
                    width = ts * 0.3f;
                    height = ts * 0.75f;
                    break;
                case PieceType.Rook:
                    width = ts * 0.4f;
                    height = ts * 0.7f;
                    break;
                case PieceType.Knight:
                    width = ts * 0.35f;
                    height = ts * 1.0f;
                    break;
                case PieceType.Bishop:
                    width = ts * 0.3f;
                    height = ts * 0.95f;
                    break;
                case PieceType.Queen:
                    width = ts * 0.35f;
                    height = ts * 0.95f;
                    break;
                case PieceType.King:
                    width = ts * 0.35f;
                    height = ts * 1.2f;
                    break;
                default:
                    width = ts * 0.3f;
                    height = ts * 0.7f;
                    break;
            }

            collider.size = new Vector3(width, height, width);
            collider.center = new Vector3(0f, height * 0.5f, 0f);
        }

        /// <summary>
        /// Removes colliders from child primitives so only the root BoxCollider is used for raycasting.
        /// </summary>
        private static void RemoveChildColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(col);
            }
        }
    }
}
