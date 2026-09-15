using UnityEngine;

public static class FurnitureProceduralMeshGenerator
{
    public static void GenerateModel(GameObject root, string label, Vector3 size, Material material)
    {
        // Clear previous visual children
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name.StartsWith("Part_"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        string upper = label.ToUpperInvariant();

        if (upper.Contains("TABLE") || upper.Contains("DESK"))
        {
            BuildTable(root, size, material);
        }
        else if (upper.Contains("COUCH") || upper.Contains("SOFA"))
        {
            BuildCouch(root, size, material);
        }
        else if (upper.Contains("CHAIR"))
        {
            BuildChair(root, size, material);
        }
        else if (upper.Contains("BED"))
        {
            BuildBed(root, size, material);
        }
        else if (upper.Contains("STORAGE") || upper.Contains("BOOKSHELF") || upper.Contains("CABINET") || upper.Contains("WARDROBE"))
        {
            BuildStorage(root, size, material);
        }
        else if (upper.Contains("SCREEN") || upper.Contains("TV") || upper.Contains("MONITOR"))
        {
            BuildScreen(root, size, material);
        }
        else if (upper.Contains("LAMP"))
        {
            BuildLamp(root, size, material);
        }
        else if (upper.Contains("PLANT"))
        {
            BuildPlant(root, size, material);
        }
        else
        {
            // Generic Box with beveled/inset look
            BuildGenericFurniture(root, size, material);
        }
    }

    private static GameObject CreatePart(GameObject parent, string name, PrimitiveType prim, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(prim);
        part.name = "Part_" + name;
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        // Visual part does not need individual collider, parent manages it
        Collider c = part.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        if (mat != null)
        {
            part.GetComponent<MeshRenderer>().material = mat;
        }

        return part;
    }

    private static void BuildTable(GameObject root, Vector3 size, Material mat)
    {
        float topThickness = Mathf.Min(0.06f, size.y * 0.15f);
        float legRadius = Mathf.Min(0.045f, Mathf.Min(size.x, size.z) * 0.08f);
        float legHeight = size.y - topThickness;

        // Table Top
        CreatePart(root, "Top", PrimitiveType.Cube, 
            new Vector3(0, (size.y * 0.5f) - (topThickness * 0.5f), 0), 
            new Vector3(size.x, topThickness, size.z), mat);

        // 4 Legs
        float insetX = size.x * 0.42f;
        float insetZ = size.z * 0.42f;
        float legPosY = -(topThickness * 0.5f);

        CreatePart(root, "Leg_FL", PrimitiveType.Cylinder, new Vector3( insetX, legPosY,  insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
        CreatePart(root, "Leg_FR", PrimitiveType.Cylinder, new Vector3(-insetX, legPosY,  insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
        CreatePart(root, "Leg_BL", PrimitiveType.Cylinder, new Vector3( insetX, legPosY, -insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
        CreatePart(root, "Leg_BR", PrimitiveType.Cylinder, new Vector3(-insetX, legPosY, -insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
    }

    private static void BuildCouch(GameObject root, Vector3 size, Material mat)
    {
        float seatHeight = size.y * 0.45f;
        float seatDepth = size.z * 0.75f;
        float backThickness = size.z * 0.25f;
        float armWidth = size.x * 0.12f;

        // Main Base / Cushion
        CreatePart(root, "SeatCushion", PrimitiveType.Cube, 
            new Vector3(0, -(size.y * 0.5f) + (seatHeight * 0.5f), (size.z * 0.5f) - (seatDepth * 0.5f)), 
            new Vector3(size.x - (armWidth * 1.8f), seatHeight, seatDepth), mat);

        // Backrest
        CreatePart(root, "Backrest", PrimitiveType.Cube, 
            new Vector3(0, 0, -(size.z * 0.5f) + (backThickness * 0.5f)), 
            new Vector3(size.x, size.y, backThickness), mat);

        // Armrests Left & Right
        float armHeight = size.y * 0.7f;
        CreatePart(root, "Arm_L", PrimitiveType.Cube, 
            new Vector3(-(size.x * 0.5f) + (armWidth * 0.5f), -(size.y * 0.5f) + (armHeight * 0.5f), 0), 
            new Vector3(armWidth, armHeight, size.z), mat);

        CreatePart(root, "Arm_R", PrimitiveType.Cube, 
            new Vector3( (size.x * 0.5f) - (armWidth * 0.5f), -(size.y * 0.5f) + (armHeight * 0.5f), 0), 
            new Vector3(armWidth, armHeight, size.z), mat);
    }

    private static void BuildChair(GameObject root, Vector3 size, Material mat)
    {
        float seatHeight = size.y * 0.48f;
        float seatThickness = Mathf.Min(0.05f, size.y * 0.1f);
        float legHeight = seatHeight - seatThickness;
        float legRadius = 0.03f;
        float backThickness = 0.04f;

        // Seat Pad
        CreatePart(root, "Seat", PrimitiveType.Cube, 
            new Vector3(0, -(size.y * 0.5f) + seatHeight - (seatThickness * 0.5f), 0), 
            new Vector3(size.x, seatThickness, size.z), mat);

        // Backrest
        float backHeight = size.y - seatHeight;
        CreatePart(root, "Backrest", PrimitiveType.Cube, 
            new Vector3(0, (size.y * 0.5f) - (backHeight * 0.5f), -(size.z * 0.5f) + (backThickness * 0.5f)), 
            new Vector3(size.x * 0.9f, backHeight, backThickness), mat);

        // 4 Legs
        float insetX = size.x * 0.4f;
        float insetZ = size.z * 0.4f;
        float legPosY = -(size.y * 0.5f) + (legHeight * 0.5f);

        CreatePart(root, "Leg_FL", PrimitiveType.Cylinder, new Vector3( insetX, legPosY,  insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
        CreatePart(root, "Leg_FR", PrimitiveType.Cylinder, new Vector3(-insetX, legPosY,  insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
        CreatePart(root, "Leg_BL", PrimitiveType.Cylinder, new Vector3( insetX, legPosY, -insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
        CreatePart(root, "Leg_BR", PrimitiveType.Cylinder, new Vector3(-insetX, legPosY, -insetZ), new Vector3(legRadius, legHeight * 0.5f, legRadius), mat);
    }

    private static void BuildBed(GameObject root, Vector3 size, Material mat)
    {
        float frameHeight = size.y * 0.35f;
        float mattressHeight = size.y * 0.35f;
        float headboardThickness = size.z * 0.1f;

        // Frame
        CreatePart(root, "Frame", PrimitiveType.Cube, 
            new Vector3(0, -(size.y * 0.5f) + (frameHeight * 0.5f), 0), 
            new Vector3(size.x, frameHeight, size.z), mat);

        // Mattress
        CreatePart(root, "Mattress", PrimitiveType.Cube, 
            new Vector3(0, -(size.y * 0.5f) + frameHeight + (mattressHeight * 0.5f), (headboardThickness * 0.5f)), 
            new Vector3(size.x * 0.95f, mattressHeight, size.z - headboardThickness), mat);

        // Headboard
        CreatePart(root, "Headboard", PrimitiveType.Cube, 
            new Vector3(0, 0, -(size.z * 0.5f) + (headboardThickness * 0.5f)), 
            new Vector3(size.x, size.y, headboardThickness), mat);

        // Pillows
        float pillowW = size.x * 0.38f;
        float pillowH = 0.08f;
        float pillowD = size.z * 0.2f;
        CreatePart(root, "Pillow_L", PrimitiveType.Cube, 
            new Vector3(-size.x * 0.24f, -(size.y * 0.5f) + frameHeight + mattressHeight + (pillowH * 0.5f), -(size.z * 0.5f) + headboardThickness + (pillowD * 0.6f)), 
            new Vector3(pillowW, pillowH, pillowD), mat);
        CreatePart(root, "Pillow_R", PrimitiveType.Cube, 
            new Vector3( size.x * 0.24f, -(size.y * 0.5f) + frameHeight + mattressHeight + (pillowH * 0.5f), -(size.z * 0.5f) + headboardThickness + (pillowD * 0.6f)), 
            new Vector3(pillowW, pillowH, pillowD), mat);
    }

    private static void BuildStorage(GameObject root, Vector3 size, Material mat)
    {
        float shelfThickness = 0.03f;
        float wallThickness = 0.035f;

        // Outer Cabinet Shell
        // Left Wall
        CreatePart(root, "Wall_L", PrimitiveType.Cube, new Vector3(-(size.x * 0.5f) + (wallThickness * 0.5f), 0, 0), new Vector3(wallThickness, size.y, size.z), mat);
        // Right Wall
        CreatePart(root, "Wall_R", PrimitiveType.Cube, new Vector3( (size.x * 0.5f) - (wallThickness * 0.5f), 0, 0), new Vector3(wallThickness, size.y, size.z), mat);
        // Top Wall
        CreatePart(root, "Wall_T", PrimitiveType.Cube, new Vector3(0, (size.y * 0.5f) - (shelfThickness * 0.5f), 0), new Vector3(size.x, shelfThickness, size.z), mat);
        // Bottom Wall
        CreatePart(root, "Wall_B", PrimitiveType.Cube, new Vector3(0, -(size.y * 0.5f) + (shelfThickness * 0.5f), 0), new Vector3(size.x, shelfThickness, size.z), mat);
        // Back Panel
        CreatePart(root, "BackPanel", PrimitiveType.Cube, new Vector3(0, 0, -(size.z * 0.5f) + (wallThickness * 0.5f)), new Vector3(size.x, size.y, wallThickness), mat);

        // Middle Shelves (2 interior horizontal dividers)
        CreatePart(root, "Shelf_1", PrimitiveType.Cube, new Vector3(0, -size.y * 0.16f, 0), new Vector3(size.x - (wallThickness * 2), shelfThickness, size.z * 0.92f), mat);
        CreatePart(root, "Shelf_2", PrimitiveType.Cube, new Vector3(0,  size.y * 0.16f, 0), new Vector3(size.x - (wallThickness * 2), shelfThickness, size.z * 0.92f), mat);
    }

    private static void BuildScreen(GameObject root, Vector3 size, Material mat)
    {
        float bezelThick = 0.03f;
        float standHeight = size.y * 0.3f;

        // Main Panel Display
        CreatePart(root, "DisplayPanel", PrimitiveType.Cube, 
            new Vector3(0, (standHeight * 0.4f), 0), 
            new Vector3(size.x, size.y - standHeight, bezelThick), mat);

        // Stand Neck
        CreatePart(root, "StandNeck", PrimitiveType.Cylinder, 
            new Vector3(0, -(size.y * 0.5f) + (standHeight * 0.5f), 0), 
            new Vector3(0.04f, standHeight * 0.5f, 0.04f), mat);

        // Stand Base
        CreatePart(root, "StandBase", PrimitiveType.Cube, 
            new Vector3(0, -(size.y * 0.5f) + 0.015f, 0), 
            new Vector3(size.x * 0.4f, 0.03f, size.z * 0.5f), mat);
    }

    private static void BuildLamp(GameObject root, Vector3 size, Material mat)
    {
        float baseHeight = 0.03f;
        float shadeHeight = size.y * 0.35f;

        // Base
        CreatePart(root, "LampBase", PrimitiveType.Cylinder, 
            new Vector3(0, -(size.y * 0.5f) + (baseHeight * 0.5f), 0), 
            new Vector3(size.x * 0.5f, baseHeight * 0.5f, size.z * 0.5f), mat);

        // Pole
        float poleHeight = size.y - baseHeight - shadeHeight;
        CreatePart(root, "LampPole", PrimitiveType.Cylinder, 
            new Vector3(0, -(size.y * 0.5f) + baseHeight + (poleHeight * 0.5f), 0), 
            new Vector3(0.03f, poleHeight * 0.5f, 0.03f), mat);

        // Shade
        CreatePart(root, "LampShade", PrimitiveType.Cylinder, 
            new Vector3(0, (size.y * 0.5f) - (shadeHeight * 0.5f), 0), 
            new Vector3(size.x * 0.85f, shadeHeight * 0.5f, size.z * 0.85f), mat);
    }

    private static void BuildPlant(GameObject root, Vector3 size, Material mat)
    {
        float potHeight = size.y * 0.4f;

        // Pot
        CreatePart(root, "PlantPot", PrimitiveType.Cylinder, 
            new Vector3(0, -(size.y * 0.5f) + (potHeight * 0.5f), 0), 
            new Vector3(size.x * 0.6f, potHeight * 0.5f, size.z * 0.6f), mat);

        // Foliage Spheres
        float folHeight = size.y - potHeight;
        CreatePart(root, "Foliage_Center", PrimitiveType.Sphere, 
            new Vector3(0, (size.y * 0.5f) - (folHeight * 0.45f), 0), 
            new Vector3(size.x * 0.8f, folHeight * 0.85f, size.z * 0.8f), mat);

        CreatePart(root, "Foliage_Top", PrimitiveType.Sphere, 
            new Vector3(0, (size.y * 0.5f) - (folHeight * 0.15f), 0), 
            new Vector3(size.x * 0.5f, folHeight * 0.5f, size.z * 0.5f), mat);
    }

    private static void BuildGenericFurniture(GameObject root, Vector3 size, Material mat)
    {
        CreatePart(root, "MainBody", PrimitiveType.Cube, Vector3.zero, size, mat);
    }
}
