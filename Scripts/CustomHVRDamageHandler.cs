using FishNet;
using HurricaneVR.Framework.Components;
using UnityEngine;
using UnityEngine.Events;

public class CustomHVRDamageHandler : HVRDamageHandler
{
    public UnityEvent<float, Vector3, Vector3> DamageTaken = new UnityEvent<float, Vector3, Vector3>();
    private NetworkDamageHandler _networkDamageHandler;

    private void Awake()
    {
        _networkDamageHandler = GetComponent<NetworkDamageHandler>();
    }

    public override void TakeDamage(float damage)
    {
        //Only the server sends damage taken event
        if (InstanceFinder.IsServer)
        {
            //Debug.Log("Server Damage");
            base.TakeDamage(damage);
        }
    }
    //Clients should call this to update the health from damage
    public virtual void TakeNetworkDamage(float damage, Vector3 hitPoint, Vector3 direction)
    {
        //Debug.Log("Network Damage");
        base.TakeDamage(damage);
    }

    public override void HandleDamageProvider(HVRDamageProvider damageProvider, Vector3 hitPoint, Vector3 direction)
    {
        if (_networkDamageHandler.IsServer)
        {
            base.HandleDamageProvider(damageProvider, hitPoint, direction);
            DamageTaken.Invoke(damageProvider.Damage, hitPoint, direction);
        }
    }

}