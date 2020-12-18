﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.EventSystems
{
    //3 things taken off to allow for indivodial camera raycasting to work, isfocused, isuppressedinthisfram, onclick removed
    [AddComponentMenu("Event/StandaloneInputModule_XR")]
    /// <summary>
    /// A BaseInputModule designed for mouse / keyboard / controller input.
    /// </summary>
    /// <remarks>
    /// Input module for working with, mouse, keyboard, or controller.
    /// </remarks>

    public class StandaloneInputModule_XR : PointerInputModule
    {

        private GameObject m_CurrentFocusedGameObject;

       [HideInInspector]public GameObject current_inputSourceGO;
      //  private Transform curLineRender_Trans;

        [Header("Camera Event RayCast Custom Made Variables")]
        [SerializeField]private float lengthOfDefaultLine = 25f;

        [HideInInspector]
        public Vector3 currentCollisionLocation;

        public Transform cursor;

        public Trigger_EventInputSource currentInputSource;

        public List<Trigger_EventInputSource> registeredInputSourceList = new List<Trigger_EventInputSource>();

        /// <summary>
        /// Set CameraEvent Source for processing ui detection and line rendering updating
        /// </summary>
        /// <param name="eventCamera"></param>
        public void RegisterInputSource(Trigger_EventInputSource inputSource, bool setAsCurrentInputSource = true)//Camera eventCamera)
        {
            //dont alternate to inactive input source
            if (!inputSource.gameObject.activeInHierarchy)
                return;

            current_inputSourceGO = inputSource.gameObject;

            //change our input camera source
            customBasedInput.controllerCameraRay = inputSource.eventCamera;

            //add any new registered sources to give default values to when not active
            if (!registeredInputSourceList.Contains(inputSource))
                registeredInputSourceList.Add(inputSource);

            if(setAsCurrentInputSource)
            currentInputSource = inputSource;

        }

        public void RemoveInputSource(Trigger_EventInputSource inputSource)
        {
            if(registeredInputSourceList.Contains(inputSource))
            registeredInputSourceList.Remove(inputSource);
        }

        private CustomBaseInput customBasedInput;
        //override unitys event system with our custom cameras
        protected override void Start()
        {
            //add default if it is not set in editor
            if (currentInputSource == null)
            {
                Debug.LogError("MISSING InputSource on StandaloneInputModule_XR.cs", gameObject);
                currentInputSource = FindObjectOfType<Trigger_EventInputSource>();
            }
            

            //force gathering of references to avoid null errors
            currentInputSource.Awake();

            //override our input
            gameObject.AddComponent<CustomBaseInput>().controllerCameraRay = currentInputSource.eventCamera;
           
            //get reference
            customBasedInput = gameObject.GetComponent<CustomBaseInput>();

            base.Start();
        }

        public override void Process()
        {
            //return if there is no imput source to process
            if (registeredInputSourceList.Count == 0)
                return;

            bool usedEvent = false;

            //send updates to selectedGameObject
            if (eventSystem.currentSelectedGameObject != null)
            {
                usedEvent = SendUpdateEventToSelectedObject();
                SendUpdateEventForCursorHover();

            }

            //obtain camera look at info to be able to work with the Unity Event System
            var pointerEvent = GetMousePointerEventData(0).GetButtonState(PointerEventData.InputButton.Left).eventData.buttonData;

            //  SendUpdateEventForCollisionInfoAndCursorUpdate(curLineRender_Trans, pointerEvent);


            if (currentInputSource != null)
            {
                SendUpdateEventForCollisionInfoAndCursorUpdate(currentInputSource.thisTransform, pointerEvent);
                SendUpdateEventToDisplayLineRenderAndSelectNewTargets(pointerEvent);

            }

            //send updates to comonents with ISliderHover interface
            SendUpdateEvenForSliderDrag();



        }


        private void SendUpdateEventForCollisionInfoAndCursorUpdate(Transform lineStart, PointerEventData pEvent)
        {
            currentCollisionLocation = lineStart.position + (lineStart.forward * (pEvent.pointerCurrentRaycast.distance + 0.025f));
            cursor.position = currentCollisionLocation;
            cursor.rotation = (Quaternion.FromToRotation(cursor.up, pEvent.pointerCurrentRaycast.worldNormal)) * cursor.rotation;
        }

        private void SendUpdateEventToDisplayLineRenderAndSelectNewTargets(PointerEventData pEvent)
        {
            //our current selection object
            var currentOverGo = pEvent.pointerCurrentRaycast.gameObject;


            foreach (var source in registeredInputSourceList)
            {

                // we use the activeState of the cursor to know if we should display our selection laser to only use it when interacting with the menu
                if (cursor.gameObject.activeInHierarchy)
                {
                    //active hand
                    if (source == currentInputSource)
                    {
                        //set default location for line
                        source.UpdateLineRenerer(source.thisTransform.position, source.thisTransform.position + (source.thisTransform.forward * lengthOfDefaultLine));

                        //if we are detecting an object
                        if (currentOverGo)
                            source.UpdateLineRenerer(source.thisTransform.position, currentCollisionLocation);

                    }//unactive hand
                    else
                    {
                        //if it is on and is not the active one show default line length
                        source.UpdateLineRenerer(source.thisTransform.position, source.thisTransform.position + (source.thisTransform.forward * lengthOfDefaultLine));
                    }

                }
                else
                    source.UpdateLineRenerer(Vector3.zero, Vector3.zero);

            }

            //check for new buttons to highlight
            if (currentOverGo != pEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pEvent, null);
                HandlePointerExitAndEnter(pEvent, currentOverGo);

                m_CurrentFocusedGameObject = pEvent.pointerCurrentRaycast.gameObject;
                eventSystem.SetSelectedGameObject(m_CurrentFocusedGameObject);
            }



        }


        protected bool SendUpdateEventToSelectedObject()
        {
            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        protected bool SendUpdateEventForCursorHover()
        {
         
            CursorHoverEventData data;

            data = new CursorHoverEventData(
                                    EventSystem.current,
                                    currentCollisionLocation, current_inputSourceGO.activeInHierarchy);

            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, CursorHoverEventData.cursorFollowDelegate);
            return data.used;
        }

        protected bool SendUpdateEvenForSliderDrag()
        {
            //if (!isUpdating)
            //    return false;
            //     var data = GetBaseEventData();
            //if (!current_LineRenderer.gameObject.activeInHierarchy)
            //    isUpdating = false;

            SliderEventData data;

            data = new SliderEventData(
                                    EventSystem.current,
                                    currentCollisionLocation, current_inputSourceGO.activeInHierarchy);

            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, SliderEventData.cursorFollowDelegate);
            return data.used;
        }

        protected bool Send_Drag_UpdateEventToSelectedObject()
        {
            BaseEventData data = GetBaseEventData();

            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.dragHandler);

            return data.used;

        }

        //use this to fire click process for lazer
        public void SetTriggerForClick()
        {
            //is there a UI element that our event camera is pointing at
            if (m_CurrentFocusedGameObject == null)
                return;

            //get the event data of our camera
            var leftButtonData = GetMousePointerEventData(0).GetButtonState(PointerEventData.InputButton.Left).eventData;
            var pointerEvent = leftButtonData.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            var newPressed = ExecuteEvents.GetEventHandler<IPointerDownHandler>(currentOverGo);
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, leftButtonData.buttonData, ExecuteEvents.pointerDownHandler);

            newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
            ExecuteEvents.Execute(newPressed, leftButtonData.buttonData, ExecuteEvents.pointerClickHandler);
        }


        protected GameObject GetCurrentFocusedGameObject()
        {
            return m_CurrentFocusedGameObject;
        }
    }
}