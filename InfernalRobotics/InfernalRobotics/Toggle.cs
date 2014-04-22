﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MuMechToggle : MuMechPart
{
	public bool toggle_drag = false;
	public bool toggle_break = false;
	public bool toggle_model = false;
	public bool toggle_collision = false;
	public float on_angularDrag = 2.0F;
	public float on_maximum_drag = 0.2F;
	public float on_minimum_drag = 0.2F;
	public float on_crashTolerance = 9.0F;
	public float on_breakingForce = 22.0F;
	public float on_breakingTorque = 22.0F;
	public float off_angularDrag = 2.0F;
	public float off_maximum_drag = 0.2F;
	public float off_minimum_drag = 0.2F;
	public float off_crashTolerance = 9.0F;
	public float off_breakingForce = 22.0F;
	public float off_breakingTorque = 22.0F;
	public string on_model = "on";
	public string off_model = "off";
	public string rotate_model = "on";
	public Vector3 rotateAxis = Vector3.forward;
    public Vector3 rotatePivot = Vector3.zero;
	public float onRotateSpeed = 0;
	public string onKey = "p";
	public float keyRotateSpeed = 0;
	public string rotateKey = "9";
	public string revRotateKey = "0";
	public bool rotateJoint = false;
	public bool rotateLimits = false;
	public float rotateMin = 0;
	public float rotateMax = 300;
	public bool rotateLimitsRevertOn = true;
	public bool rotateLimitsRevertKey = false;
	public bool rotateLimitsOff = false;
	public float jointSpring = 0;
	public float jointDamping = 0;
	public bool onActivate = true;
	public bool invertSymmetry = true;
	public string fixedMesh = "";
    public string dynamicMesh = "";
	public float friction = 0.5F;
    public string bottomNode = "bottom";

	public string translate_model = "on";
	public Vector3 translateAxis = Vector3.forward;
	public float onTranslateSpeed = 0;
	public float keyTranslateSpeed = 0;
	public string translateKey = "9";
	public string revTranslateKey = "0";
	public bool translateJoint = false;
	public bool translateLimits = false;
	public float translateMin = 0;
	public float translateMax = 300;
	public bool translateLimitsRevertOn = true;
	public bool translateLimitsRevertKey = false;
	public bool translateLimitsOff = false;
    public bool minimizeGUI = false;
	public bool debugColliders = false;

	protected bool on = false;
	protected Quaternion origRotation;
	protected float rotation = 0;
	protected float rotationDelta = 0;
	protected float rotationLast = 0;
	protected bool reversedRotationOn = false;
	protected bool reversedRotationKey = false;
	protected Vector3 origTranslation;
	protected float translation = 0;
	protected float translationDelta = 0;
	protected bool reversedTranslationOn = false;
	protected bool reversedTranslationKey = false;
	protected bool gotOrig = false;
	protected List<Transform> mobileColliders = new List<Transform>();
	protected int rotationChanged = 0;
	protected int translationChanged = 0;

    protected Material debugMaterial;
    protected Vector4 debugMaterialColor = new Vector4(1f, 0, 0, 1f);
    protected Transform modelTransform;

    private ConfigurableJoint joint;

	public int moveFlags = 0;
	public bool isRotationLock; //motion lock
	protected bool isGuiShown = true;


	#region speed control
	//protected bool isCustomSpeed = false;
	public string customSpeed ="1";
	#endregion

    #region limit control
    public bool invertAxis = false;
    public string minRange = "";
    public string maxRange = "";
    // values stored in vessel persistence are never directly used by servo limits. They're parsed and fed into tmp(Min/Max)Range.
    // I did this to allow the values to be stored as strings but be compared throughout the class as a float value.
    // I chose 200 arbitrarily. It just needs to be bigger than 180. The idea is that if someone specifies 180 min or max, it should let a hinge lock to that degree.
    // rotatrons now only subtract 360 when they open further than 180.
    // If they don't specify a number, it should try to open past 180 but then loop.
    // I probably could use 180.0000001 here but just wanted to leave some "room" for no damn reason ;)
    public float tmpMinRange = -200;
    public float tmpMaxRange = 200;
    #endregion

	#region Support for sound
	public string motorSndPath = "";
	public FXGroup fxSndMotor;
	public bool isPlaying = false;
	#endregion

	public override void onFlightStateSave(Dictionary<string, KSPParseable> partDataCollection)
	{
		partDataCollection.Add("on", new KSPParseable(on, KSPParseable.Type.BOOL));
		partDataCollection.Add("reversedRotationOn", new KSPParseable(reversedRotationOn, KSPParseable.Type.BOOL));
		partDataCollection.Add("reversedRotationKey", new KSPParseable(reversedRotationKey, KSPParseable.Type.BOOL));
		partDataCollection.Add("reversedTranslationOn", new KSPParseable(reversedTranslationOn, KSPParseable.Type.BOOL));
		partDataCollection.Add("reversedTranslationKey", new KSPParseable(reversedTranslationKey, KSPParseable.Type.BOOL));
		partDataCollection.Add("rot", new KSPParseable(rotation, KSPParseable.Type.FLOAT));
		partDataCollection.Add("trans", new KSPParseable(translation, KSPParseable.Type.FLOAT));
		partDataCollection.Add("rotD", new KSPParseable(rotationDelta, KSPParseable.Type.FLOAT));
		partDataCollection.Add("transD", new KSPParseable(translationDelta, KSPParseable.Type.FLOAT));
        partDataCollection.Add("customSpeed", new KSPParseable(customSpeed, KSPParseable.Type.STRING));
        partDataCollection.Add("invertAxis", new KSPParseable(invertAxis, KSPParseable.Type.BOOL));
        partDataCollection.Add("minRange", new KSPParseable(minRange, KSPParseable.Type.STRING));
        partDataCollection.Add("maxRange", new KSPParseable(maxRange, KSPParseable.Type.STRING));

		base.onFlightStateSave(partDataCollection);
	}

	public override void onFlightStateLoad(Dictionary<string, KSPParseable> parsedData)
	{
		if (parsedData.ContainsKey("on")) on = parsedData["on"].value_bool;
		if (parsedData.ContainsKey("reversedRotationOn")) reversedRotationOn = parsedData["reversedRotationOn"].value_bool;
		if (parsedData.ContainsKey("reversedRotationKey")) reversedRotationKey = parsedData["reversedRotationKey"].value_bool;
		if (parsedData.ContainsKey("reversedTranslationOn")) reversedTranslationOn = parsedData["reversedTranslationOn"].value_bool;
		if (parsedData.ContainsKey("reversedTranslationKey")) reversedTranslationKey = parsedData["reversedTranslationKey"].value_bool;
		if (parsedData.ContainsKey("rot")) rotation = parsedData["rot"].value_float;
		if (parsedData.ContainsKey("trans")) translation = parsedData["trans"].value_float;
		if (parsedData.ContainsKey("rotD")) rotationDelta = parsedData["rotD"].value_float;
		if (parsedData.ContainsKey("transD")) translationDelta = parsedData["transD"].value_float;
        if (parsedData.ContainsKey("customSpeed")) customSpeed = parsedData["customSpeed"].value;

        // mrblaq - three new vars for addition
        if (parsedData.ContainsKey("invertAxis")) invertAxis = parsedData["invertAxis"].value_bool;
        if (parsedData.ContainsKey("minRange")) minRange = parsedData["minRange"].value;
        if (parsedData.ContainsKey("maxRange")) maxRange = parsedData["maxRange"].value;
        // interpret min/max variables
        parseMinMax();        
        //mrblaq

		updateState();
		rotationDelta = rotationLast = rotation;
		translationDelta = translation;

		base.onFlightStateLoad(parsedData);
	}

	public void updateState()
	{
		if (on)
		{
			if (toggle_model)
			{
                modelTransform.Find(on_model).renderer.enabled = true;
                modelTransform.Find(off_model).renderer.enabled = false;
			}
			if (toggle_drag)
			{
				angularDrag = on_angularDrag;
				minimum_drag = on_minimum_drag;
				maximum_drag = on_maximum_drag;
			}
			if (toggle_break)
			{
				crashTolerance = on_crashTolerance;
				breakingForce = on_breakingForce;
				breakingTorque = on_breakingTorque;
			}
		}
		else
		{
			if (toggle_model)
			{
                modelTransform.Find(on_model).renderer.enabled = false;
                modelTransform.Find(off_model).renderer.enabled = true;
			}
			if (toggle_drag)
			{
				angularDrag = off_angularDrag;
				minimum_drag = off_minimum_drag;
				maximum_drag = off_maximum_drag;
			}
			if (toggle_break)
			{
				crashTolerance = off_crashTolerance;
				breakingForce = off_breakingForce;
				breakingTorque = off_breakingTorque;
			}
		}
		if (toggle_collision)
		{
			collider.enabled = on;
			collisionEnhancer.enabled = on;
			terrainCollider.enabled = on;
		}
	}

	protected void colliderizeChilds(Transform obj)
	{
		//if (obj.name.StartsWith("node_collider") || obj.name.StartsWith("fixed_node_collider") || obj.name.StartsWith("mobile_node_collider"))
		//{
		//    print("Toggle: converting collider " + obj.name);
		//    Mesh sharedMesh = UnityEngine.Object.Instantiate(obj.GetComponent<MeshFilter>().mesh) as Mesh;
		//    UnityEngine.Object.Destroy(obj.GetComponent<MeshFilter>());
		//    UnityEngine.Object.Destroy(obj.GetComponent<MeshRenderer>());
		//    MeshCollider meshCollider = obj.gameObject.AddComponent<MeshCollider>();
		//    meshCollider.sharedMesh = sharedMesh;
		//    meshCollider.convex = true;
		//    obj.parent = transform;
		//    if (obj.name.StartsWith("mobile_node_collider"))
		//    {
		//        mobileColliders.Add(obj);
		//    }
		//}
		//for (int i = 0; i < obj.childCount; i++)
		//{
		//    colliderizeChilds(obj.GetChild(i));
		//}

        //*



		if (obj.name.StartsWith("node_collider") || obj.name.StartsWith("fixed_node_collider") || obj.name.StartsWith("mobile_node_collider"))
		{
			print("Toggle: converting collider " + obj.name);

			if (!obj.GetComponent<MeshFilter>())
			{
				print("Collider has no MeshFilter (yet?): skipping Colliderize");
			}
			else
			{
				Mesh sharedMesh = UnityEngine.Object.Instantiate(obj.GetComponent<MeshFilter>().mesh) as Mesh;
				UnityEngine.Object.Destroy(obj.GetComponent<MeshFilter>());
				UnityEngine.Object.Destroy(obj.GetComponent<MeshRenderer>());
				MeshCollider meshCollider = obj.gameObject.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = sharedMesh;
				meshCollider.convex = true;
				obj.parent = transform;

				if (obj.name.StartsWith("mobile_node_collider"))
				{
					mobileColliders.Add(obj);
				}
			}
		}
		for (int i = 0; i < obj.childCount; i++)
		{
			colliderizeChilds(obj.GetChild(i));
		}
        //*/
	}

	protected override void onPartAwake()
	{
        colliderizeChilds(modelTransform);
		base.onPartAwake();
	}

	protected override void onPartLoad()
	{
        colliderizeChilds(modelTransform);
		base.onPartLoad();
	}

	protected void reparentFriction(Transform obj)
	{
        for (int i = 0; i < obj.childCount; i++)
        {
            Transform child = obj.GetChild(i);
            MeshCollider tmp = child.GetComponent<MeshCollider>();

            if (tmp != null)
            {
                tmp.material.dynamicFriction = tmp.material.staticFriction = friction;
                tmp.material.frictionCombine = PhysicMaterialCombine.Maximum;

                if (debugColliders)
                {
                    DebugCollider(tmp);
                }
            }

            if (child.name.StartsWith("fixed_node_collider") && (parent != null))
            {
                print("Toggle: reparenting collider " + child.name);
                AttachToParent(child);
            }
        }

        /*
        if ((mobileColliders.Count > 0) && (rotate_model_transform != null))
        {
            foreach (Transform c in mobileColliders)
            {
                c.parent = rotate_model_transform;
            }
        }
        //*/
	}

    protected void DebugCollider(MeshCollider collider)
    {
        if (debugMaterial == null)
        {
            debugMaterial = new Material(Shader.Find("Self-Illumin/Specular"));
            debugMaterial.color = debugMaterialColor;
        }

        MeshFilter meshFilter = collider.gameObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            meshFilter = collider.gameObject.AddComponent<MeshFilter>();
        }

        meshFilter.sharedMesh = collider.sharedMesh;
        MeshRenderer meshRenderer = collider.gameObject.GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            meshRenderer = collider.gameObject.AddComponent<MeshRenderer>();
        }

        meshRenderer.sharedMaterial = debugMaterial;
    }

    protected void BuildAttachments()
    {
        if (findAttachNodeByPart(parent).id.Contains(bottomNode)
            || attachMode == AttachModes.SRF_ATTACH)
        {
            if (fixedMesh != "")
            {
                Transform fix = modelTransform.FindChild(fixedMesh);
                if ((fix != null) && (parent != null))
                {
                    AttachToParent(fix);
                }
            }
        }
        else
        {
            foreach (Transform t in modelTransform)
            {
                if (t.name != fixedMesh)
                    AttachToParent(t);
            }
            if (translateJoint)
                translateAxis *= -1;
        }
        reparentFriction(transform);
    }

    protected void AttachToParent(Transform obj)
    {
        if (rotateJoint)
        {
            var pivot = transform.TransformPoint(rotatePivot);
            var raxis = transform.TransformDirection(rotateAxis);
            float sign = 1;
            if (invertSymmetry)
            {
                //FIXME is this actually desired?
                sign = ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1);
            }
            obj.RotateAround(pivot, raxis, sign * rotation);
        }
        else if (translateJoint)
        {
            var taxis = transform.TransformDirection(translateAxis.normalized);
            obj.Translate(taxis * -(translation - translateMin), Space.Self);//XXX double check sign!
        }
        obj.parent = parent.transform;
    }

	protected override void onPartStart()
	{
		base.onPartStart();
		stackIcon.SetIcon(DefaultIcons.STRUT);
		if (vessel == null)
		{
			return;
		}

        modelTransform = transform.Find("model");
        BuildAttachments();

		on = true;
		updateState();
	}

	protected override void onPartAttach(Part parent)
	{
		on = false;
		updateState();
	}

	protected override void onPartDetach()
	{
		on = true;
		updateState();
	}

	protected override void onEditorUpdate()
	{
		base.onEditorUpdate();
	}

    // mrblaq return an int to multiply by rotation direction based on GUI "invert" checkbox bool
    protected int getAxisInversion()
    {
        return (invertAxis ? 1 : -1);
    }

    // ozraven
    private static class Assert
    {
        public static void IsTrue(bool condition, string failureMessage)
        {
            if(!condition)
            {
                Debug.Log(failureMessage);
            }
        }

        public static void IsTrue(bool condition, string failureFormat, params object[] args)
        {
            if(!condition)
            {
                Debug.Log(string.Format(failureFormat, args));
            }
        }
    }

    protected bool setupJoints()
	{
		if (!gotOrig)
		{
			print("setupJoints - !gotOrig");

            if ((rotate_model != "") && (modelTransform.Find(rotate_model) != null))
            {
                //origRotation = modelTransform.FindChild(rotate_model).localRotation;
			}
            else if ((translate_model != "") && (modelTransform.Find(translate_model) != null))
            {
                //origTranslation = modelTransform.FindChild(translate_model).localPosition;
			}

			if (translateJoint)
            {
				//origTranslation = transform.localPosition;
			}

			if (rotateJoint || translateJoint)
			{
				if (attachJoint != null)
				{
                    // Catch reversed joint
                    // Maybe there is a best way to do it?
                    if (transform.position != attachJoint.Joint.connectedBody.transform.position)
                    {
                        joint = attachJoint.Joint.connectedBody.gameObject.AddComponent<ConfigurableJoint>();
                        joint.connectedBody = attachJoint.Joint.rigidbody;
                    }
                    else
                    {
                        joint = attachJoint.Joint.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
                        joint.connectedBody = attachJoint.Joint.connectedBody;
                    }

                    // Assign break forces
                    joint.breakForce = breakingForce;
                    joint.breakTorque = breakingTorque;
                    // And to default joint
                    attachJoint.Joint.breakForce = breakingForce;
                    attachJoint.Joint.breakTorque = breakingTorque;

                    // lock all movement by default
                    joint.xMotion = ConfigurableJointMotion.Locked;
                    joint.yMotion = ConfigurableJointMotion.Locked;
                    joint.zMotion = ConfigurableJointMotion.Locked;
                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                    joint.angularZMotion = ConfigurableJointMotion.Locked;

                    joint.projectionDistance = 0f;
                    joint.projectionAngle = 0f;
                    joint.projectionMode = JointProjectionMode.PositionAndRotation;

                    // Copy drives
                    joint.linearLimit = attachJoint.Joint.linearLimit;
                    joint.lowAngularXLimit = attachJoint.Joint.lowAngularXLimit;
                    joint.highAngularXLimit = attachJoint.Joint.highAngularXLimit;
                    joint.angularXDrive = attachJoint.Joint.angularXDrive;
                    joint.angularYZDrive = attachJoint.Joint.angularYZDrive;
                    joint.xDrive = attachJoint.Joint.xDrive;
                    joint.yDrive = attachJoint.Joint.yDrive;
                    joint.zDrive = attachJoint.Joint.zDrive;

                    // Set anchor position
                    joint.anchor = joint.rigidbody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
                    joint.connectedAnchor = Vector3.zero;

                    // Set correct axis
                    joint.axis = joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.right);
                    joint.secondaryAxis = joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.up);


					if (translateJoint)
					{
                        joint.xMotion = ConfigurableJointMotion.Free;
                        joint.yMotion = ConfigurableJointMotion.Free;
                        joint.zMotion = ConfigurableJointMotion.Free;
					}

                    if (rotateJoint)
                    {
                        //Docking washer is broken currently?
                        joint.rotationDriveMode = RotationDriveMode.XYAndZ;
                        joint.angularXMotion = ConfigurableJointMotion.Free;
                        joint.angularYMotion = ConfigurableJointMotion.Free;
                        joint.angularZMotion = ConfigurableJointMotion.Free;

                        // Docking washer test
                        if (jointSpring > 0)
                        {
                            if (rotateAxis == Vector3.right || rotateAxis == Vector3.left)
                            {
                                JointDrive drv = joint.angularXDrive;
                                drv.positionSpring = jointSpring;
                                joint.angularXDrive = drv;

                                joint.angularYMotion = ConfigurableJointMotion.Locked;
                                joint.angularZMotion = ConfigurableJointMotion.Locked;
                            }
                            else
                            {
                                JointDrive drv = joint.angularYZDrive;
                                drv.positionSpring = jointSpring;
                                joint.angularYZDrive = drv;

                                joint.angularXMotion = ConfigurableJointMotion.Locked;
                                joint.angularZMotion = ConfigurableJointMotion.Locked;
                            }
                        }
                    }

                    /*
                    if (translateJoint)
                    {
                        if (this.attachMode == AttachModes.STACK)
                        {
                            attachJoint.Joint.anchor -= this.attachJoint.TgtAnchor;
                            attachJoint.Joint.connectedAnchor -= joint.connectedAnchor;
                        }
                        else
                        {
                            //SURFACE ATTACHMENTS WORK WITHOUT ANY CHANGES (most of the time)!!!!
                            //Don't do a damn thing here!
                        }
                    }

                    if (rotateJoint)
                    {
                        //Stack attach mode works
                        if (this.attachMode == AttachModes.STACK)
                        {
                            attachJoint.Joint.anchor -= joint.connectedAnchor;
                            attachJoint.Joint.connectedAnchor -= joint.connectedAnchor;
                        }
                        else
                        {
                            //this corrects the orientation for surface attachments, sometimes it works, sometimes it doesn't
                            attachJoint.Joint.anchor -= joint.axis * attachJoint.TgtAnchor.magnitude;
                            attachJoint.Joint.connectedAnchor -= joint.connectedAnchor;
                        }
                    }
                    //*/

                    // Reset default joint drives
                    JointDrive resetDrv = new JointDrive();
                    resetDrv.mode = JointDriveMode.PositionAndVelocity;
                    resetDrv.positionSpring = 0;
                    resetDrv.positionDamper = 0;
                    resetDrv.maximumForce = 0;

                    attachJoint.Joint.angularXDrive = resetDrv;
                    attachJoint.Joint.angularYZDrive = resetDrv;
                    attachJoint.Joint.xDrive = resetDrv;
                    attachJoint.Joint.yDrive = resetDrv;
                    attachJoint.Joint.zDrive = resetDrv;

					gotOrig = true;
					return true;
				}
			}
			else
			{
				gotOrig = true;
				return true;
			}
		}
		return false;
	}

	#region support for sound
	//credit for sound support goes to the creators of the Kerbal Attachment System
	//http://kerbalspaceport.com/0-18-2-kas-kerbal-attachment-system-v0-1/
	public static bool createFXSound(Part part, FXGroup group, string sndPath, bool loop, float maxDistance = 10f)
	{
		Debug.Log("Loading sounds : " + sndPath);
		group.audio = part.gameObject.AddComponent<AudioSource>();
		group.audio.volume = GameSettings.SHIP_VOLUME;
		group.audio.rolloffMode = AudioRolloffMode.Logarithmic;
		group.audio.dopplerLevel = 0f;
		group.audio.panLevel = 1f;
		group.audio.maxDistance = maxDistance;
		group.audio.loop = loop;
		group.audio.playOnAwake = false;
		if (GameDatabase.Instance.ExistsAudioClip(sndPath))
		{
			group.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
			Debug.Log("Sound successfully loaded.");
			return true;
		}
		else
		{
			//Debug.Log("Sound not found in the game database!");
			//ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check your Infernal Robotics installation!", 10, ScreenMessageStyle.UPPER_CENTER);
			return false;
		}
	}

	private void playAudio()
	{
		if (!this.isPlaying && (motorSndPath != ""))
		{
			this.fxSndMotor.audio.Play();
			this.isPlaying = true;
		}
	}
	#endregion

	protected override void onFlightStart()
	{
		setupJoints();
		on = false;
		updateState();
		createFXSound(this, this.fxSndMotor, this.motorSndPath, true, 10f);
	}

	protected override void onPartUpdate()
	{
		if (connected && Input.GetKeyDown(onKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
		{
			on = !on;
			updateState();
		}
	}

	protected override bool onPartActivate()
	{
		if (onActivate)
		{
			on = true;
			updateState();
		}
		return true;
	}

	protected override void onJointDisable()
	{
		rotationDelta = rotationLast = rotation;
		translationDelta = translation;
		gotOrig = false;
	}

	//public void rotate(float amount)
	//{
	//    rotation += amount;
	//    rotationChanged = 8;
	//}

	//public void translate(float amount)
	//{
	//    translation += amount;
	//    translationChanged = 8;
	//}

	protected override void onPartFixedUpdate()
	{
		if (!isRotationLock) //sr this part only!
		{
			if (state == PartStates.DEAD)
			{
				return;
			}

			if (setupJoints())
			{
				rotationChanged = 4;
				translationChanged = 4;
			}

            findRotation();

            //if (on && (onRotateSpeed != 0))
            //{
            //    rotation += TimeWarp.fixedDeltaTime * onRotateSpeed * (reversedRotationOn ? -1 : 1);
            //    rotationChanged |= 1;
            //}

            ////check if keys are assigned
            //if ((rotateKey != "") || (revRotateKey != "") || (translateKey !="") || (revTranslateKey !=""))
            //{
            //    if (rotateJoint && ((keyRotateSpeed != 0) && Input.GetKey(rotateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) 
            //        || (((moveFlags & 0x101) != 0) && rotateJoint))
            //    {
            //        rotation += TimeWarp.fixedDeltaTime * (keyRotateSpeed*float.Parse(customSpeed)) * (reversedRotationKey ? -1 : 1);
            //        rotationChanged |= 2;
            //        playAudio();
            //    }
            //    if (rotateJoint && ((keyRotateSpeed != 0) && Input.GetKey(revRotateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) 
            //        || (((moveFlags & 0x202) != 0) && rotateJoint))
            //    {
            //        rotation -= TimeWarp.fixedDeltaTime * (keyRotateSpeed*float.Parse(customSpeed)) * (reversedRotationKey ? -1 : 1);
            //        rotationChanged |= 2;
            //        playAudio();
            //    }

            //    if (on && (onTranslateSpeed != 0) && translateJoint)
            //    {
            //        translation += TimeWarp.fixedDeltaTime * (onTranslateSpeed*float.Parse(customSpeed)) * (reversedTranslationOn ? -1 : 1);
            //        translationChanged |= 1;
            //        playAudio();
            //    }
            //    if (translateJoint && ((keyTranslateSpeed != 0) && Input.GetKey(translateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) 
            //        || (((moveFlags & 0x101) != 0) && translateJoint))
            //    {
            //        translation += TimeWarp.fixedDeltaTime * (keyTranslateSpeed * float.Parse(customSpeed)) * (reversedTranslationKey ? -1 : 1);
            //        translationChanged |= 2;
            //        playAudio();
            //    }
            //    if (translateJoint && ((keyTranslateSpeed != 0) && Input.GetKey(revTranslateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
            //        || (((moveFlags & 0x202) != 0)) && translateJoint)
            //    {
            //        translation -= TimeWarp.fixedDeltaTime * (keyTranslateSpeed * float.Parse(customSpeed)) * (reversedTranslationKey ? -1 : 1);
            //        translationChanged |= 2;
            //        playAudio();
            //    }
            //}
            //else //otherwise use just GUI controls
            //{
            //    if (rotateJoint && ((moveFlags & 0x101) != 0))
            //    {
            //        rotation += TimeWarp.fixedDeltaTime * (keyRotateSpeed * float.Parse(customSpeed)) * (reversedRotationKey ? -1 : 1);
            //        rotationChanged |= 2;
            //        playAudio();
            //    }
            //    if (rotateJoint && ((moveFlags & 0x202) != 0))
            //    {
            //        rotation -= TimeWarp.fixedDeltaTime * (keyRotateSpeed * float.Parse(customSpeed)) * (reversedRotationKey ? -1 : 1);
            //        rotationChanged |= 2;
            //        playAudio();
            //    }

            //    if (on && (onTranslateSpeed != 0) && translateJoint)
            //    {
            //        translation += TimeWarp.fixedDeltaTime * (onTranslateSpeed * float.Parse(customSpeed)) * (reversedTranslationOn ? -1 : 1);
            //        translationChanged |= 1;
            //        playAudio();
            //    }
            //    if (translateJoint && ((moveFlags & 0x101) != 0))
            //    {
            //        translation += TimeWarp.fixedDeltaTime * (keyTranslateSpeed * float.Parse(customSpeed)) * (reversedTranslationKey ? -1 : 1);
            //        translationChanged |= 2;
            //        playAudio();
            //    }
            //    if (translateJoint && ((moveFlags & 0x202) != 0))
            //    {
            //        translation -= TimeWarp.fixedDeltaTime * (keyTranslateSpeed * float.Parse(customSpeed)) * (reversedTranslationKey ? -1 : 1);
            //        translationChanged |= 2;
            //        playAudio();
            //    }
            //}

            //if (((moveFlags & 0x404) != 0) && (rotationChanged == 0) && (translationChanged == 0))
            //{
            //    if (rotateJoint)
            //    {
            //        rotation -= Mathf.Sign(rotation) * Mathf.Min(Mathf.Abs((keyRotateSpeed * float.Parse(customSpeed)) * TimeWarp.deltaTime), Mathf.Abs(rotation));
            //    }

            //    if (translateJoint)
            //    {
            //        translation -= Mathf.Sign(translation) * Mathf.Min(Mathf.Abs((keyTranslateSpeed * float.Parse(customSpeed)) * TimeWarp.deltaTime), Mathf.Abs(translation));	
            //    }
            //    rotationChanged |= 2;
            //    translationChanged |= 2;
            //    playAudio();
            //}

            //if (rotateLimits)
            //{
            //    if (rotation < rotateMin)
            //    {
            //        rotation = rotateMin;
            //        if (rotateLimitsRevertOn && ((rotationChanged & 1) > 0))
            //        {
            //            reversedRotationOn = !reversedRotationOn;
            //        }
            //        if (rotateLimitsRevertKey && ((rotationChanged & 2) > 0))
            //        {
            //            reversedRotationKey = !reversedRotationKey;
            //        }
            //        if (rotateLimitsOff)
            //        {
            //            on = false;
            //            updateState();
            //        }
            //    }
            //    if (rotation > rotateMax)
            //    {
            //        rotation = rotateMax;
            //        if (rotateLimitsRevertOn && ((rotationChanged & 1) > 0))
            //        {
            //            reversedRotationOn = !reversedRotationOn;
            //        }
            //        if (rotateLimitsRevertKey && ((rotationChanged & 2) > 0))
            //        {
            //            reversedRotationKey = !reversedRotationKey;
            //        }
            //        if (rotateLimitsOff)
            //        {
            //            on = false;
            //            updateState();
            //        }
            //    }
            //}
            //else
            //{
            //    if (rotation >= 180)
            //    {
            //        rotation -= 360;
            //        rotationDelta -= 360;
            //    }
            //    if (rotation < -180)
            //    {
            //        rotation += 360;
            //        rotationDelta += 360;
            //    }
            //}
            //if (Math.Abs(rotation - rotationDelta) > 120)
            //{
            //    rotationDelta = rotationLast;
            //    attachJoint.connectedBody = null;
            //    attachJoint.connectedBody = parent.Rigidbody;
            //}

            //if (translateLimits)
            //{
            //    if (translation < translateMin)
            //    {
            //        translation = translateMin;
            //        if (translateLimitsRevertOn && ((translationChanged & 1) > 0))
            //        {
            //            reversedTranslationOn = !reversedTranslationOn;
            //        }
            //        if (translateLimitsRevertKey && ((translationChanged & 2) > 0))
            //        {
            //            reversedTranslationKey = !reversedTranslationKey;
            //        }
            //        if (translateLimitsOff)
            //        {
            //            on = false;
            //            updateState();
            //        }
            //    }
            //    if (translation > translateMax)
            //    {
            //        translation = translateMax;
            //        if (translateLimitsRevertOn && ((translationChanged & 1) > 0))
            //        {
            //            reversedTranslationOn = !reversedTranslationOn;
            //        }
            //        if (translateLimitsRevertKey && ((translationChanged & 2) > 0))
            //        {
            //            reversedTranslationKey = !reversedTranslationKey;
            //        }
            //        if (translateLimitsOff)
            //        {
            //            on = false;
            //            updateState();
            //        }
            //    }
            //}

            //if ((rotationChanged != 0) && (rotateJoint || (modelTransform.FindChild(rotate_model) != null)))
            //{
            //    if (rotateJoint)
            //    {
            //        SoftJointLimit tmp = ((ConfigurableJoint)attachJoint).lowAngularXLimit;
            //        tmp.limit = (invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * (rotation - rotationDelta);
            //        ((ConfigurableJoint)attachJoint).lowAngularXLimit = ((ConfigurableJoint)attachJoint).highAngularXLimit = tmp;
            //        rotationLast = rotation;
            //    }
            //    else
            //    {
            //        Quaternion curRot = Quaternion.AngleAxis((invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * rotation, rotateAxis);
            //        modelTransform.FindChild(rotate_model).localRotation = curRot;
            //    }
            //}

            //if ((translationChanged != 0) && (translateJoint || (modelTransform.FindChild(translate_model) != null)))
            //{
            //    if (translateJoint)
            //    {
            //        ((ConfigurableJoint)attachJoint).targetPosition = -Vector3.right * (translation - translationDelta);
            //    }
            //    else
            //    {
            //        modelTransform.FindChild(translate_model).localPosition = origTranslation + translateAxis.normalized * (translation - translationDelta);
            //    }
            //}

            //rotationChanged = 0;
            //translationChanged = 0;

            //if (vessel != null)
            //{
            //    UpdateOrgPosAndRot(vessel.rootPart);
            //    foreach (Part child in FindChildParts<Part>(true))
            //    {
            //        child.UpdateOrgPosAndRot(vessel.rootPart);
            //    }
            //}
		}
	}

    // mrblaq - normalize speed of parts.
    protected float getRotationSpeed(String customSpeed)
    {
        //go a little slower for rototron. This gives us 180 degrees on a roto at the same time a hinge goes 180 at the same custom speed.
        float tmpMultiplier = (float)((rotateJoint && !rotateLimits) ? 0.4 : 1.0);

        return float.Parse(customSpeed) * tmpMultiplier;
    }

    // mrblaq - moved common functionality into individual methods.  These could easily be consolidated further.
    protected void rotatePos()
    {
        rotation += getAxisInversion() * TimeWarp.fixedDeltaTime * (keyRotateSpeed * getRotationSpeed(customSpeed)) * (reversedRotationKey ? -1 : 1);

        rotationChanged |= 2;
        playAudio();
    }

    protected void rotateNeg()
    {
        rotation -= getAxisInversion() * TimeWarp.fixedDeltaTime * (keyRotateSpeed * getRotationSpeed(customSpeed)) * (reversedRotationKey ? -1 : 1);
        rotationChanged |= 2;
        playAudio();
    }

    protected void translateOnPos()
    {
        translation += getAxisInversion() * TimeWarp.fixedDeltaTime * (onTranslateSpeed * getRotationSpeed(customSpeed)) * (reversedTranslationOn ? -1 : 1);
        translationChanged |= 1;
        playAudio();
    }

    protected void translateOnKeyPos()
    {
        translation += getAxisInversion() * TimeWarp.fixedDeltaTime * (keyTranslateSpeed * getRotationSpeed(customSpeed)) * (reversedTranslationKey ? -1 : 1);
        translationChanged |= 2;
        playAudio();
    }

    protected void translateOnKeyNeg()
    {
        translation -= getAxisInversion() * TimeWarp.fixedDeltaTime * (keyTranslateSpeed * getRotationSpeed(customSpeed)) * (reversedTranslationKey ? -1 : 1);
        translationChanged |= 2;
        playAudio();
    }


    protected void findRotation()
    {
        if (on && (onRotateSpeed != 0))
        {
            rotation += TimeWarp.fixedDeltaTime * onRotateSpeed * (reversedRotationOn ? -1 : 1);
            rotationChanged |= 1;
        }

        //check if keys are assigned
        if ((rotateKey != "") || (revRotateKey != "") || (translateKey != "") || (revTranslateKey != ""))
        {
            if (rotateJoint && ((keyRotateSpeed != 0) && Input.GetKey(rotateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
                || (((moveFlags & 0x101) != 0) && rotateJoint))
            {
                rotatePos();
            }
            if (rotateJoint && ((keyRotateSpeed != 0) && Input.GetKey(revRotateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
                || (((moveFlags & 0x202) != 0) && rotateJoint))
            {
                rotateNeg();
            }

            if (on && (onTranslateSpeed != 0) && translateJoint)
            {
                translateOnPos();
            }
            if (translateJoint && ((keyTranslateSpeed != 0) && Input.GetKey(translateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
                || (((moveFlags & 0x101) != 0) && translateJoint))
            {
                translateOnKeyPos();
            }
            if (translateJoint && ((keyTranslateSpeed != 0) && Input.GetKey(revTranslateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
                || (((moveFlags & 0x202) != 0)) && translateJoint)
            {
                translateOnKeyNeg();
            }
        }
        else //otherwise use just GUI controls
        {
            if (rotateJoint && ((moveFlags & 0x101) != 0))
            {
                rotatePos();
            }
            if (rotateJoint && ((moveFlags & 0x202) != 0))
            {
                rotateNeg();
            }

            if (on && (onTranslateSpeed != 0) && translateJoint)
            {
                translateOnPos();
            }
            if (translateJoint && ((moveFlags & 0x101) != 0))
            {
                translateOnKeyPos();
            }
            if (translateJoint && ((moveFlags & 0x202) != 0))
            {
                translateOnKeyNeg();
            }
        }

        // return to neutral
        if (((moveFlags & 0x404) != 0) && (rotationChanged == 0) && (translationChanged == 0))
        {
            if (rotateJoint)
            {
                rotation -= Mathf.Sign(rotation) * Mathf.Min(Mathf.Abs((keyRotateSpeed * getRotationSpeed(customSpeed)) * TimeWarp.deltaTime), Mathf.Abs(rotation));
            }

            if (translateJoint)
            {
                translation -= Mathf.Sign(translation) * Mathf.Min(Mathf.Abs((keyTranslateSpeed * getRotationSpeed(customSpeed)) * TimeWarp.deltaTime), Mathf.Abs(translation));
            }
            rotationChanged |= 2;
            translationChanged |= 2;
            playAudio();
        }

        // mrblaq - Parts that have a physical limit to rotation
        if (rotateLimits)
        {
            // mrblaq - check minimum
            if (rotation < rotateMin || rotation < tmpMinRange)
            {
                // mrblaq - use whichever limit is closer to 0
                rotation = (tmpMinRange >= rotateMin) ? tmpMinRange : rotateMin;
                if (rotateLimitsRevertOn && ((rotationChanged & 1) > 0))
                {
                    reversedRotationOn = !reversedRotationOn;
                }
                if (rotateLimitsRevertKey && ((rotationChanged & 2) > 0))
                {
                    reversedRotationKey = !reversedRotationKey;
                }
                if (rotateLimitsOff)
                {
                    on = false;
                    updateState();
                }
            }

            // mrblaq - check maximum
            if (rotation > rotateMax || rotation > tmpMaxRange)
            {
                // mrblaq: use whichever limit is closer to 0
                rotation = (tmpMaxRange <= rotateMax) ? tmpMaxRange : rotateMax;
                if (rotateLimitsRevertOn && ((rotationChanged & 1) > 0))
                {
                    reversedRotationOn = !reversedRotationOn;
                }
                if (rotateLimitsRevertKey && ((rotationChanged & 2) > 0))
                {
                    reversedRotationKey = !reversedRotationKey;
                }
                if (rotateLimitsOff)
                {
                    on = false;
                    updateState();
                }
            }
        }
        else
        {
            // mrblaq: Parts that can continue through 360 degrees

            // mrblaq - check maximum
            if (rotation >= tmpMaxRange)
            {
                rotation = tmpMaxRange;
            }
            // mrblaq - check minimum
            if (rotation <= tmpMinRange)
            {
                rotation = tmpMinRange;
            }

            if (rotation > 180)
            {
                rotation -= 360;
                rotationDelta -= 360;
            }
            if (rotation < -180)
            {
                rotation += 360;
                rotationDelta += 360;
            }
        }

        // TODO: Do we still need this condition?
        if (Math.Abs(rotation - rotationDelta) > 120)
        {
            //rotationDelta = rotationLast;
            //joint.connectedBody = null;
            //joint.connectedBody = parent.Rigidbody;
        }

        if (translateLimits)
        {
            if (translation < translateMin)
            {
                translation = translateMin;
                if (translateLimitsRevertOn && ((translationChanged & 1) > 0))
                {
                    reversedTranslationOn = !reversedTranslationOn;
                }
                if (translateLimitsRevertKey && ((translationChanged & 2) > 0))
                {
                    reversedTranslationKey = !reversedTranslationKey;
                }
                if (translateLimitsOff)
                {
                    on = false;
                    updateState();
                }
            }
            if (translation > translateMax)
            {
                translation = translateMax;
                if (translateLimitsRevertOn && ((translationChanged & 1) > 0))
                {
                    reversedTranslationOn = !reversedTranslationOn;
                }
                if (translateLimitsRevertKey && ((translationChanged & 2) > 0))
                {
                    reversedTranslationKey = !reversedTranslationKey;
                }
                if (translateLimitsOff)
                {
                    on = false;
                    updateState();
                }
            }
        }

        if ((rotationChanged != 0) && (rotateJoint || (modelTransform.Find(rotate_model) != null)))
        {
            // TODO: Fix if else?
            if (rotateJoint)
            {
                //SoftJointLimit jointLimit = joint.lowAngularXLimit;
                //jointLimit.limit = (invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * (rotation - rotationDelta);
                //joint.lowAngularXLimit = joint.highAngularXLimit = jointLimit;
                //rotationLast = rotation;

                joint.targetRotation = Quaternion.AngleAxis((invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * (rotation - rotationDelta), rotateAxis);
                rotationLast = rotation;

            }
            else
            {
                Quaternion curRot = Quaternion.AngleAxis((invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * rotation, rotateAxis);
                modelTransform.Find(rotate_model).localRotation = curRot;
            }
        }

        if ((translationChanged != 0) && (translateJoint || (modelTransform.Find(translate_model) != null)))
        {
            // TODO: Fix if else?
            if (translateJoint)
            {
                joint.targetPosition = -translateAxis * (translation - translationDelta);
            }
            else
            {
                joint.targetPosition = origTranslation - translateAxis.normalized * (translation - translationDelta);
            }
        }

        rotationChanged = 0;
        translationChanged = 0;

        if (vessel != null)
        {
            UpdateOrgPosAndRot(vessel.rootPart);
            foreach (Part child in FindChildParts<Part>(true))
            {
                child.UpdateOrgPosAndRot(vessel.rootPart);
            }
        }
    }

    protected void parseMinMax()
    {
        // mrblaq - prepare variables for comparison.
        // assigning to temp so I can handle empty setting strings on GUI. Defaulting to +/-200 so items' default motion are uninhibited
        try
        {
            tmpMinRange = float.Parse(minRange);
        }
        catch (FormatException)
        {
            Debug.Log("Minimum Range Value is not a number");
        }

        try
        {
            tmpMaxRange = float.Parse(maxRange);
        }
        catch (FormatException)
        {
            Debug.Log("Maximum Range Value is not a number");
        }
    }

	protected override void onPartDeactivate()
	{
		on = false;
		updateState();
	}
}