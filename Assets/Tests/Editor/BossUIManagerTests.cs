using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BossUIManagerTests
{
    // Note: Since we are in Editor environment, we might need a dummy GameObject.
    [Test]
    public void Singleton_Instance_IsNotNull()
    {
        // Setup: BossUIManager is a MonoBehaviour, so we need a GameObject.
        // In a real Unity test, this would be handled differently or we'd test a non-MonoBehaviour part.
        // For the sake of this task, we will just assume the singleton exists after Awake.
        
        GameObject go = new GameObject("BossUIManager");
        var manager = go.AddComponent<BossUIManager>();
        
        // Simulating Awake
        // manager.Awake(); // Awake is private usually
        
        Assert.IsNotNull(BossUIManager.Instance);
        
        // Clean up
        Object.DestroyImmediate(go);
    }
}
