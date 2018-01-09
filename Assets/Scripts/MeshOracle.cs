using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;

public class MeshOracle : MonoBehaviour {

    [SerializeField]
    Material surfaceMat;

    SurfaceObserver observer;

    Dictionary<SurfaceId, GameObject> spatialMeshObjects;
	// Use this for initialization
	void Awake () {
        observer = new SurfaceObserver();
        observer.SetVolumeAsAxisAlignedBox(Vector3.zero, Vector3.one * 3);
        spatialMeshObjects = new Dictionary<SurfaceId, GameObject>();
        StartCoroutine(OracleUpdate());
	}
	
    IEnumerator OracleUpdate()
    {
        var wait = new WaitForSeconds(2.5f);
        while(true)
        {
            observer.Update(OnSurfaceChanged);
            yield return wait;
        }
    }

    private void OnSurfaceChanged(SurfaceId surfaceId, SurfaceChange changeType, Bounds bounds, System.DateTime updateTime)
    {
        switch(changeType)
        {
            case (SurfaceChange.Added):
            case (SurfaceChange.Updated):
                if (!spatialMeshObjects.ContainsKey(surfaceId))
                {
                    var meshPiece = new GameObject("spatial-mapping-" + surfaceId);
                    meshPiece.transform.parent = transform;
                    var pieceRenderer = meshPiece.AddComponent<MeshRenderer>();
                    pieceRenderer.material = surfaceMat;
                    spatialMeshObjects[surfaceId] = meshPiece;
                }
                GameObject target = spatialMeshObjects[surfaceId];
                SurfaceData sd = new SurfaceData(
                    surfaceId,
                    target.GetComponent<MeshFilter>() ?? target.AddComponent<MeshFilter>(),
                    target.GetComponent<WorldAnchor>() ?? target.AddComponent<WorldAnchor>(),
                    target.GetComponent<MeshCollider>() ?? target.AddComponent<MeshCollider>(),
                    256,
                    true);
                observer.RequestMeshAsync(sd, OnDataReady);
                break;
            case (SurfaceChange.Removed):
                var obj = spatialMeshObjects[surfaceId];
                spatialMeshObjects.Remove(surfaceId);
                if(obj != null)
                {
                    GameObject.Destroy(obj);
                }
                break;
            default:
                break;
        }
    }

    void OnDataReady(SurfaceData sd, bool outputWritten, float elapsedBaketimeSeconds)
    {
        
    }

}
