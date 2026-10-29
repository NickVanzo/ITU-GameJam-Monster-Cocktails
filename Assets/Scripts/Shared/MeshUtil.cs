using UnityEngine;

public static class MeshUtil
{
    // Box around every renderer under model, in space's local coordinates.
    public static Bounds MeshBounds(Transform model, Transform space)
    {
        Bounds bounds = default;
        bool bAny = false;

        foreach(Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            // TODO: Bake this
            Bounds local = renderer.localBounds;
            for(int i = 0; i < 8; ++i)
            {
                Vector3 vCorner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 vInSpace = space.InverseTransformPoint(renderer.transform.TransformPoint(vCorner));

                if(bAny)
                {
                    bounds.Encapsulate(vInSpace);
                }
                else
                {
                    bounds = new Bounds(vInSpace, Vector3.zero);
                    bAny = true;
                }
            }
        }

        return bounds;
    }

    // Turns the model the way it'd lie in a bag: thinnest side up, longest side along the grid's x.
    public static void LayFlat(Transform model, Transform holder)
    {
        Vector3 vSize = MeshBounds(model, holder).size;
        Vector3[] aAxes = { Vector3.right, Vector3.up, Vector3.forward };
        System.Array.Sort(new[] { vSize.x, vSize.y, vSize.z }, aAxes);

        // aAxes now runs thinnest to longest. This sends the thinnest to up and the middle one to forward, so the longest ends up on x.
        model.localRotation = Quaternion.Inverse(Quaternion.LookRotation(aAxes[1], aAxes[0])) * model.localRotation;
    }
}
