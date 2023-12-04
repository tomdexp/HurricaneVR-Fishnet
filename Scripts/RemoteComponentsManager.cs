using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RemoteComponentsManager : NetworkBehaviour
{
    [FormerlySerializedAs("remoteRemoveComponents")] [SerializeField]
    private List<Component> _remoteRemoveComponents;
    [FormerlySerializedAs("remoteRemoveGameObjects")] [SerializeField] 
    private List<GameObject> _remoteRemoveGameObjects;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // The local client doesn't change any components
        if (Owner.IsLocalClient)
        {
            return;
        }
        // The remote clients should remove all the components and
        // game objects that are included in the lists
        foreach (var comp in _remoteRemoveComponents)
        {
            Destroy(comp);
        }
        foreach (var go in _remoteRemoveGameObjects)
        {
            Destroy(go);
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        // The remote clients should remove all the components and
        // game objects that are included in the lists
        foreach (var comp in _remoteRemoveComponents)
        {
            Destroy(comp);
        }
        foreach (var go in _remoteRemoveGameObjects)
        {
            Destroy(go);
        }
    }
}
