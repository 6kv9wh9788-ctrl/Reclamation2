using System.Collections;
using NUnit.Framework;
using Reclamation.Neighborhood;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reclamation.Tests
{
    public sealed class LabCameraTests
    {
        private GameObject cameraObject, person, clockObject;
        private LabCameraController controller;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        [SetUp]
        public void Setup()
        {
            cameraObject = new GameObject("Test camera");
            cameraObject.transform.position = new Vector3(0, 20, -20);
            cameraObject.transform.LookAt(Vector3.zero);
            initialPosition = cameraObject.transform.position;
            initialRotation = cameraObject.transform.rotation;
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 20;
            controller = cameraObject.AddComponent<LabCameraController>();
            controller.InputEnabled = false;
            person = new GameObject("Follow target");
            clockObject = new GameObject("Paused clock");
            clockObject.AddComponent<NeighborhoodClock>().SetPaused(true);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(person);
            Object.DestroyImmediate(clockObject);
        }

        [UnityTest]
        public IEnumerator FollowTracksMovingTargetWhileClockIsPaused()
        {
            controller.Follow(person.transform);
            Assert.That(controller.SelectedTarget, Is.EqualTo(person.transform));
            yield return null;
            yield return null;
            Vector3 before = cameraObject.transform.position;
            person.transform.position += new Vector3(3, 0, 2);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(cameraObject.transform.position - before, new Vector3(3, 0, 2)), Is.LessThan(0.01f));
            Assert.That(clockObject.GetComponent<NeighborhoodClock>().Paused, Is.True);
        }

        [UnityTest]
        public IEnumerator SelectionDoesNotForceCameraFollow()
        {
            controller.Select(person.transform);
            yield return null;
            Assert.That(controller.SelectedTarget, Is.EqualTo(person.transform));
            Assert.That(controller.FollowTarget, Is.Null);
            controller.ClearSelection();
            Assert.That(controller.SelectedTarget, Is.Null);
            Assert.That(controller.FollowTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator DisabledTargetReleasesFollowWithoutMovingCamera()
        {
            person.transform.position = new Vector3(3, 0, 0);
            controller.Follow(person.transform);
            yield return null;
            yield return null;
            Vector3 before = cameraObject.transform.position;
            person.SetActive(false);
            yield return null;
            yield return null;
            Assert.That(controller.FollowTarget == null, Is.True);
            Assert.That(controller.SelectedTarget == null, Is.True);
            Assert.That(Vector3.Distance(cameraObject.transform.position, before), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator OverviewRestoresOriginalCameraAndZoom()
        {
            person.transform.position = new Vector3(8, 0, 5);
            controller.Follow(person.transform);
            cameraObject.GetComponent<Camera>().orthographicSize = 4;
            yield return null;
            controller.ResetView();
            yield return null;
            yield return null;
            Assert.That(controller.FollowTarget == null, Is.True);
            Assert.That(controller.SelectedTarget == null, Is.True);
            Assert.That(Vector3.Distance(cameraObject.transform.position, initialPosition), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(cameraObject.transform.rotation, initialRotation), Is.LessThan(0.01f));
            Assert.That(cameraObject.GetComponent<Camera>().orthographicSize, Is.EqualTo(20));
        }
    }
}
