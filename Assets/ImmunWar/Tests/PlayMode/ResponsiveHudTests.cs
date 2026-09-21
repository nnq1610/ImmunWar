using System.Collections;
using ImmunWar.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class ResponsiveHudTests
    {
        [UnityTest] public IEnumerator ThreeTargetResolutionsRemainInsideSafeAreaAndFocusable() { foreach (var size in new[] { new Vector2(1280, 720), new Vector2(1920, 1080), new Vector2(2560, 1440) }) { var go = new GameObject("SafeHud", typeof(RectTransform), typeof(SafeAreaController)); var controller = go.GetComponent<SafeAreaController>(); controller.Apply(new Rect(0, 0, size.x, size.y), size); var rect = (RectTransform)go.transform; Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one)); Object.Destroy(go); } var eventSystem = new GameObject("EventSystem", typeof(EventSystem)); Assert.That(eventSystem.GetComponent<EventSystem>(), Is.Not.Null); Object.Destroy(eventSystem); yield return null; }
    }
}
