using FishNet.Object;
using HurricaneVR.Framework.Components;
using UnityEngine;

public class NetworkDestructible : NetworkBehaviour
{
    private CustomHVRDestructible _hvrDestructible;
    private bool _isDestroyed;
    private bool _isServer;

    private void Awake()
    {
        _hvrDestructible = GetComponent<CustomHVRDestructible>();
        if (_hvrDestructible == null)
        {
            Debug.LogError("NetworkDestructible requires a CustomHVRDestructible component, " +
                           "click on the message to select the GameObject with the issue", this);
            return;
        }
        _hvrDestructible.BeforeDestroy.AddListener(OnBeforeDestroy);
    }

    private void OnBeforeDestroy()
    {
        if (!_isDestroyed)
        {
            if (!_isServer)
            {
                RPCDestroy();
            }
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        _isServer = true;
        Destroy(_hvrDestructible);
    }
    private void OnDestroy()
    {
        if(_hvrDestructible != null) _hvrDestructible.BeforeDestroy.RemoveListener(OnBeforeDestroy);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RPCDestroy()
    {
        //Tell observers about destroy
        ObserversDestroy();
        Destroy(gameObject);
    }

    [ObserversRpc]
    private void ObserversDestroy()
    {
        //Destroy the destructible on the observer clients that still have it
        if (_hvrDestructible != null && !_isDestroyed)
        {
            _isDestroyed = true;
            _hvrDestructible.Destroy();
        }
    }
}
