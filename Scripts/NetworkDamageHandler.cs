using FishNet.Object;
using UnityEngine;

public class NetworkDamageHandler : NetworkBehaviour
{
    private CustomHVRDamageHandler _hvrDamageHandler;

    private void Awake()
    {
        _hvrDamageHandler = GetComponent<CustomHVRDamageHandler>();
        _hvrDamageHandler.DamageTaken.AddListener(OnDamageTaken);
    }
    private void OnDestroy()
    {
        if (_hvrDamageHandler) _hvrDamageHandler.DamageTaken.RemoveListener(OnDamageTaken);
    }

    private void OnDamageTaken(float damage, Vector3 hitPoint, Vector3 direction)
    {
        RPCDamageTaken(damage, hitPoint, direction);
    }

    //The server should be the only one sending damage and is already updated
    [ObserversRpc(ExcludeServer = true)]
    private void RPCDamageTaken(float damage, Vector3 hitPoint, Vector3 direction)
    {
        _hvrDamageHandler.TakeNetworkDamage(damage, hitPoint, direction);
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        //Client damage handlers should never trigger destruction, it will be handled in NetworkDestructible
        _hvrDamageHandler.Desctructible = null;
    }
}
