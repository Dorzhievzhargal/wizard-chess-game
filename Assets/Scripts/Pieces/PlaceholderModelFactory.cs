using UnityEngine;
using WizardChess.Core;
using WizardChess.Visual;

namespace WizardChess.Pieces
{
    /// <summary>
    /// Static factory that creates detailed placeholder 3D models using Unity primitives
    /// for each chess piece type. Each type has a visually distinct composite shape
    /// built from multiple named primitives for material categorization.
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

            // Try to load a 3D model prefab first; fall back to procedural primitives
            bool usedPrefab = false;
            if (type == PieceType.Pawn)
            {
                usedPrefab = TryBuildFromPrefab(root, "Models/Pawn/Paladin WProp J Nordstrom@Rifle Turn", tileSize, "PawnAnimator", color);
            }

            if (!usedPrefab)
            {
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
            }

            AddRaycastCollider(root, type, tileSize);

            return root;
        }

        /// <summary>
        /// Attempts to instantiate a 3D model from a prefab/FBX and parent it to root.
        /// Returns true if successful.
        /// </summary>
        private static bool TryBuildFromPrefab(GameObject root, string resourcePath, float tileSize, string animatorName, PieceColor color)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"PlaceholderModelFactory: Could not load prefab at '{resourcePath}', falling back to primitives.");
                return false;
            }

            var model = Object.Instantiate(prefab, root.transform);
            model.name = "Model";

            // Scale the model to fit the tile (Mixamo models are ~1.8m tall, we want ~0.8 tile units)
            float targetHeight = tileSize * 0.8f;
            float modelHeight = 1.8f; // approximate Mixamo humanoid height
            float scaleFactor = targetHeight / modelHeight;
            model.transform.localScale = Vector3.one * scaleFactor;
            model.transform.localPosition = Vector3.zero;
            // White faces toward black (positive Z), Black faces toward white (negative Z)
            float yRotation = color == PieceColor.White ? 0f : 180f;
            model.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            // Assign Animator Controller if available
            var animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrEmpty(animatorName))
            {
                var controller = Resources.Load<RuntimeAnimatorController>(animatorName);
                if (controller == null)
                    controller = Resources.Load<RuntimeAnimatorController>("Models/Pawn/" + animatorName);
                if (controller != null)
                    animator.runtimeAnimatorController = controller;
            }

            // Remove child colliders from the model
            foreach (var col in model.GetComponentsInChildren<Collider>())
                Object.Destroy(col);

            return true;
        }

        /// <summary>
        /// Pawn: small armored soldier with spear.
        /// 6 primitives: Base, Body, ShoulderArmor, Head, Helmet, Spear.
        /// </summary>
        private static void BuildPawn(GameObject root, float ts)
        {
            // Base — wide flat cylinder pedestal
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.30f, ts * 0.06f, ts * 0.30f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.06f, 0f);

            // Body — capsule torso
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.22f, ts * 0.25f, ts * 0.22f);
            body.transform.localPosition = new Vector3(0f, ts * 0.30f, 0f);

            // ShoulderArmor — flattened cube across shoulders
            var shoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoulder.name = "ShoulderArmor";
            shoulder.transform.SetParent(root.transform, false);
            shoulder.transform.localScale = new Vector3(ts * 0.28f, ts * 0.06f, ts * 0.16f);
            shoulder.transform.localPosition = new Vector3(0f, ts * 0.48f, 0f);

            // Head — small sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.16f;
            head.transform.localPosition = new Vector3(0f, ts * 0.58f, 0f);

            // Helmet — slightly larger flattened sphere on top of head
            var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmet.name = "Helmet";
            helmet.transform.SetParent(root.transform, false);
            helmet.transform.localScale = new Vector3(ts * 0.18f, ts * 0.10f, ts * 0.18f);
            helmet.transform.localPosition = new Vector3(0f, ts * 0.66f, 0f);

            // Spear — thin tall cylinder held to the side
            var spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spear.name = "Spear";
            spear.transform.SetParent(root.transform, false);
            spear.transform.localScale = new Vector3(ts * 0.03f, ts * 0.40f, ts * 0.03f);
            spear.transform.localPosition = new Vector3(ts * 0.14f, ts * 0.45f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Rook: heavy stone tower golem.
        /// 8 primitives: Base, BodyLower, BodyUpper, Merlon_0–3, Platform.
        /// </summary>
        private static void BuildRook(GameObject root, float ts)
        {
            // Base — wide flat cylinder
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.40f, ts * 0.06f, ts * 0.40f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.06f, 0f);

            // BodyLower — wide cube lower section
            var bodyLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyLower.name = "BodyLower";
            bodyLower.transform.SetParent(root.transform, false);
            bodyLower.transform.localScale = new Vector3(ts * 0.36f, ts * 0.22f, ts * 0.36f);
            bodyLower.transform.localPosition = new Vector3(0f, ts * 0.23f, 0f);

            // BodyUpper — slightly narrower cube upper section
            var bodyUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyUpper.name = "BodyUpper";
            bodyUpper.transform.SetParent(root.transform, false);
            bodyUpper.transform.localScale = new Vector3(ts * 0.32f, ts * 0.18f, ts * 0.32f);
            bodyUpper.transform.localPosition = new Vector3(0f, ts * 0.43f, 0f);

            // Platform — flat cube on top of body
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.SetParent(root.transform, false);
            platform.transform.localScale = new Vector3(ts * 0.36f, ts * 0.04f, ts * 0.36f);
            platform.transform.localPosition = new Vector3(0f, ts * 0.54f, 0f);

            // 4 Merlons — small cubes on corners of the platform
            float mSize = ts * 0.10f;
            float mY = ts * 0.62f;
            float mOffset = ts * 0.13f;
            Vector3[] mPositions = {
                new Vector3(-mOffset, mY, -mOffset),
                new Vector3(mOffset, mY, -mOffset),
                new Vector3(-mOffset, mY, mOffset),
                new Vector3(mOffset, mY, mOffset)
            };
            for (int i = 0; i < mPositions.Length; i++)
            {
                var merlon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                merlon.name = $"Merlon_{i}";
                merlon.transform.SetParent(root.transform, false);
                merlon.transform.localScale = Vector3.one * mSize;
                merlon.transform.localPosition = mPositions[i];
            }

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Knight: mounted horse warrior.
        /// 8 primitives: Base, HorseBody, Neck, Head, Muzzle, EarL, EarR, Mane.
        /// </summary>
        private static void BuildKnight(GameObject root, float ts)
        {
            // Base — flat cylinder pedestal
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.32f, ts * 0.06f, ts * 0.32f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.06f, 0f);

            // HorseBody — capsule torso
            var horseBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            horseBody.name = "HorseBody";
            horseBody.transform.SetParent(root.transform, false);
            horseBody.transform.localScale = new Vector3(ts * 0.26f, ts * 0.28f, ts * 0.26f);
            horseBody.transform.localPosition = new Vector3(0f, ts * 0.30f, 0f);

            // Neck — cylinder tilted forward (horse neck)
            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform, false);
            neck.transform.localScale = new Vector3(ts * 0.14f, ts * 0.20f, ts * 0.14f);
            neck.transform.localPosition = new Vector3(0f, ts * 0.62f, ts * 0.06f);
            neck.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);

            // Head — sphere (horse head)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.18f;
            head.transform.localPosition = new Vector3(0f, ts * 0.80f, ts * 0.14f);

            // Muzzle — small elongated cube for the horse snout
            var muzzle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muzzle.name = "Muzzle";
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localScale = new Vector3(ts * 0.10f, ts * 0.08f, ts * 0.14f);
            muzzle.transform.localPosition = new Vector3(0f, ts * 0.76f, ts * 0.24f);

            // EarL — small cube (left ear)
            var earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earL.name = "EarL";
            earL.transform.SetParent(root.transform, false);
            earL.transform.localScale = new Vector3(ts * 0.04f, ts * 0.08f, ts * 0.04f);
            earL.transform.localPosition = new Vector3(-ts * 0.06f, ts * 0.92f, ts * 0.14f);

            // EarR — small cube (right ear)
            var earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earR.name = "EarR";
            earR.transform.SetParent(root.transform, false);
            earR.transform.localScale = new Vector3(ts * 0.04f, ts * 0.08f, ts * 0.04f);
            earR.transform.localPosition = new Vector3(ts * 0.06f, ts * 0.92f, ts * 0.14f);

            // Mane — flattened cube along the back of the neck
            var mane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mane.name = "Mane";
            mane.transform.SetParent(root.transform, false);
            mane.transform.localScale = new Vector3(ts * 0.04f, ts * 0.22f, ts * 0.10f);
            mane.transform.localPosition = new Vector3(0f, ts * 0.72f, -ts * 0.04f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Bishop: battle mage with staff and robes.
        /// 8 primitives: Base, RobedBody, Head, Mitre, Staff, StaffOrb, ShoulderCapeL, ShoulderCapeR.
        /// </summary>
        private static void BuildBishop(GameObject root, float ts)
        {
            // Base — flat cylinder pedestal
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.30f, ts * 0.06f, ts * 0.30f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.06f, 0f);

            // RobedBody — tall tapered cylinder (robed torso)
            var robedBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            robedBody.name = "RobedBody";
            robedBody.transform.SetParent(root.transform, false);
            robedBody.transform.localScale = new Vector3(ts * 0.20f, ts * 0.32f, ts * 0.20f);
            robedBody.transform.localPosition = new Vector3(0f, ts * 0.38f, 0f);

            // Head — sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.16f;
            head.transform.localPosition = new Vector3(0f, ts * 0.74f, 0f);

            // Mitre — pointed hat (vertically stretched sphere)
            var mitre = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mitre.name = "Mitre";
            mitre.transform.SetParent(root.transform, false);
            mitre.transform.localScale = new Vector3(ts * 0.10f, ts * 0.14f, ts * 0.10f);
            mitre.transform.localPosition = new Vector3(0f, ts * 0.88f, 0f);

            // Staff — thin tall cylinder to the side
            var staff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            staff.name = "Staff";
            staff.transform.SetParent(root.transform, false);
            staff.transform.localScale = new Vector3(ts * 0.03f, ts * 0.45f, ts * 0.03f);
            staff.transform.localPosition = new Vector3(ts * 0.16f, ts * 0.45f, 0f);

            // StaffOrb — small sphere at top of staff
            var staffOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            staffOrb.name = "StaffOrb";
            staffOrb.transform.SetParent(root.transform, false);
            staffOrb.transform.localScale = Vector3.one * ts * 0.07f;
            staffOrb.transform.localPosition = new Vector3(ts * 0.16f, ts * 0.92f, 0f);

            // ShoulderCapeL — flattened cube draped on left shoulder
            var capeL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            capeL.name = "ShoulderCapeL";
            capeL.transform.SetParent(root.transform, false);
            capeL.transform.localScale = new Vector3(ts * 0.08f, ts * 0.18f, ts * 0.14f);
            capeL.transform.localPosition = new Vector3(-ts * 0.14f, ts * 0.52f, 0f);

            // ShoulderCapeR — flattened cube draped on right shoulder
            var capeR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            capeR.name = "ShoulderCapeR";
            capeR.transform.SetParent(root.transform, false);
            capeR.transform.localScale = new Vector3(ts * 0.08f, ts * 0.18f, ts * 0.14f);
            capeR.transform.localPosition = new Vector3(ts * 0.14f, ts * 0.52f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Queen: elite warrior-mage with crown and orb.
        /// 10 primitives: Base, Body, Neck, Head, CrownRing, CrownPoint_0–3, Orb.
        /// </summary>
        private static void BuildQueen(GameObject root, float ts)
        {
            // Base — wide flat cylinder pedestal
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.34f, ts * 0.06f, ts * 0.34f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.06f, 0f);

            // Body — elegant tapered capsule
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.24f, ts * 0.32f, ts * 0.24f);
            body.transform.localPosition = new Vector3(0f, ts * 0.36f, 0f);

            // Neck — thin cylinder
            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform, false);
            neck.transform.localScale = new Vector3(ts * 0.10f, ts * 0.08f, ts * 0.10f);
            neck.transform.localPosition = new Vector3(0f, ts * 0.66f, 0f);

            // Head — sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.16f;
            head.transform.localPosition = new Vector3(0f, ts * 0.78f, 0f);

            // CrownRing — flattened cylinder ring around head
            var crownRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crownRing.name = "CrownRing";
            crownRing.transform.SetParent(root.transform, false);
            crownRing.transform.localScale = new Vector3(ts * 0.20f, ts * 0.03f, ts * 0.20f);
            crownRing.transform.localPosition = new Vector3(0f, ts * 0.86f, 0f);

            // 4 CrownPoints — small spheres rising from the crown ring
            float cpRadius = ts * 0.08f;
            float cpY = ts * 0.92f;
            Vector3[] cpPositions = {
                new Vector3(0f, cpY, cpRadius),
                new Vector3(0f, cpY, -cpRadius),
                new Vector3(cpRadius, cpY, 0f),
                new Vector3(-cpRadius, cpY, 0f)
            };
            for (int i = 0; i < cpPositions.Length; i++)
            {
                var crownPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crownPoint.name = $"CrownPoint_{i}";
                crownPoint.transform.SetParent(root.transform, false);
                crownPoint.transform.localScale = Vector3.one * ts * 0.05f;
                crownPoint.transform.localPosition = cpPositions[i];
            }

            // Orb — small sphere on top
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb";
            orb.transform.SetParent(root.transform, false);
            orb.transform.localScale = Vector3.one * ts * 0.08f;
            orb.transform.localPosition = new Vector3(0f, ts * 0.98f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// King: armored king with crown, cross, pauldrons, and sword.
        /// 10 primitives: Base, Body, Head, Crown, CrossVertical, CrossHorizontal,
        /// PauldronL, PauldronR, SwordBlade, SwordGuard.
        /// </summary>
        private static void BuildKing(GameObject root, float ts)
        {
            // Base — wide flat cylinder pedestal
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(root.transform, false);
            baseObj.transform.localScale = new Vector3(ts * 0.36f, ts * 0.07f, ts * 0.36f);
            baseObj.transform.localPosition = new Vector3(0f, ts * 0.07f, 0f);

            // Body — tall armored cylinder
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(ts * 0.24f, ts * 0.36f, ts * 0.24f);
            body.transform.localPosition = new Vector3(0f, ts * 0.46f, 0f);

            // Head — sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * ts * 0.18f;
            head.transform.localPosition = new Vector3(0f, ts * 0.86f, 0f);

            // Crown — flattened cylinder on top of head
            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Crown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localScale = new Vector3(ts * 0.22f, ts * 0.04f, ts * 0.22f);
            crown.transform.localPosition = new Vector3(0f, ts * 0.98f, 0f);

            // CrossVertical — thin vertical cube on top of crown
            var crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossV.name = "CrossVertical";
            crossV.transform.SetParent(root.transform, false);
            crossV.transform.localScale = new Vector3(ts * 0.04f, ts * 0.14f, ts * 0.04f);
            crossV.transform.localPosition = new Vector3(0f, ts * 1.10f, 0f);

            // CrossHorizontal — thin horizontal cube
            var crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossH.name = "CrossHorizontal";
            crossH.transform.SetParent(root.transform, false);
            crossH.transform.localScale = new Vector3(ts * 0.10f, ts * 0.04f, ts * 0.04f);
            crossH.transform.localPosition = new Vector3(0f, ts * 1.12f, 0f);

            // PauldronL — left shoulder armor (sphere)
            var pauldronL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pauldronL.name = "PauldronL";
            pauldronL.transform.SetParent(root.transform, false);
            pauldronL.transform.localScale = new Vector3(ts * 0.14f, ts * 0.10f, ts * 0.14f);
            pauldronL.transform.localPosition = new Vector3(-ts * 0.16f, ts * 0.74f, 0f);

            // PauldronR — right shoulder armor (sphere)
            var pauldronR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pauldronR.name = "PauldronR";
            pauldronR.transform.SetParent(root.transform, false);
            pauldronR.transform.localScale = new Vector3(ts * 0.14f, ts * 0.10f, ts * 0.14f);
            pauldronR.transform.localPosition = new Vector3(ts * 0.16f, ts * 0.74f, 0f);

            // SwordBlade — thin tall cylinder to the side
            var swordBlade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            swordBlade.name = "SwordBlade";
            swordBlade.transform.SetParent(root.transform, false);
            swordBlade.transform.localScale = new Vector3(ts * 0.03f, ts * 0.38f, ts * 0.03f);
            swordBlade.transform.localPosition = new Vector3(ts * 0.20f, ts * 0.50f, 0f);

            // SwordGuard — small horizontal cube at sword base
            var swordGuard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            swordGuard.name = "SwordGuard";
            swordGuard.transform.SetParent(root.transform, false);
            swordGuard.transform.localScale = new Vector3(ts * 0.10f, ts * 0.03f, ts * 0.05f);
            swordGuard.transform.localPosition = new Vector3(ts * 0.20f, ts * 0.30f, 0f);

            RemoveChildColliders(root);
        }

        /// <summary>
        /// Applies the appropriate color scheme to all renderers in the piece hierarchy
        /// using MaterialFactory for consistent, high-quality materials.
        /// Parts are categorized by name for accent, weapon, or base material assignment.
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

                if (IsAccentPart(partName))
                {
                    r.material = Object.Instantiate(accentMat);
                }
                else if (IsWeaponPart(partName))
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
        /// Returns true if the part name corresponds to an accent (decorative) element.
        /// </summary>
        private static bool IsAccentPart(string partName)
        {
            return partName == "Crown"
                || partName == "CrossVertical"
                || partName == "CrossHorizontal"
                || partName == "Mitre"
                || partName == "Orb"
                || partName == "CrownRing"
                || partName == "StaffOrb"
                || partName.StartsWith("CrownPoint");
        }

        /// <summary>
        /// Returns true if the part name corresponds to a weapon element.
        /// </summary>
        private static bool IsWeaponPart(string partName)
        {
            return partName == "Spear"
                || partName == "Staff"
                || partName == "SwordBlade"
                || partName == "SwordGuard";
        }

        /// <summary>
        /// Adds a single BoxCollider to the root object encompassing the piece for raycasting.
        /// </summary>
        private static void AddRaycastCollider(GameObject root, PieceType type, float ts)
        {
            var collider = root.AddComponent<BoxCollider>();

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
                    height = ts * 1.0f;
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
