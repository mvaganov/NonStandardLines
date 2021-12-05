using System.Collections;
using System.Collections.Generic;
using NonStandard;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RuntimeLineTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void EmptyTest()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator LinesOverTime()
    {
        // TODO create a camera
        Lines.Make("arrow0").Arrow(Vector3.zero, new Vector3(3, 3, 3), Color.blue);
        yield return new WaitForSeconds(20);
        yield return null;
    }
}
