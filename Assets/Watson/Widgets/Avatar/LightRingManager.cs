﻿using UnityEngine;
using System.Collections;
using IBM.Watson.Utilities;
using IBM.Watson.Logging;

namespace IBM.Watson.Widgets.Avatar
{
    /// <summary>
    /// LightRingManager manages the Avatar 3d models' animated light rings according to mood / behavior change. 
    /// </summary>
	public class LightRingManager : AvatarModelManager
    {

        #region Public Variables
        #endregion

        #region Private Variables

        #endregion

        public LeanTweenType lightFlareEase;
		public float lightFlareAnimationTime = 1.0f;
		public GameObject[] lightFlareList;
		public GameObject[] lightFlareChild;
		public Vector3[][] listFlarePathList;

		public Color colorTest;

        #region OnEnable / OnDisable / OnApplicationQuit / Awake
        
		void OnApplicationQuit()
		{
			//TODO: Go back to initial coloring
		}

        protected override void Awake(){
            base.Awake();

            listFlarePathList = new Vector3[3][];
			
			listFlarePathList [0] = new Vector3[] {
				new Vector3 (28.365f, -6.06f, -16.385f),
				new Vector3 (22.58f, -6.06f, 32.78f),
				new Vector3 (41.7f, -6.06f, 8.4f),
				new Vector3 (0.062f, -6.06f, 32.764f)
			};
			
			listFlarePathList [1] = new Vector3[] {
				new Vector3 (-28.37233f, -6.06f, -16.3723f),
				new Vector3 (17.0983f, -6.06f, -35.94485f),
				new Vector3 (-13.57539f, -6.06f, -40.31325f),
				new Vector3 (28.34345f, -6.06f, -16.4357f)
			};
			
			listFlarePathList [2] = new Vector3[] {
				new Vector3 (0.007372856f, -6.060182f, 32.75727f),
				new Vector3 (-39.67829f, -6.060145f, 3.164852f),
				new Vector3 (-28.12456f, -6.060268f, 31.91325f),
				new Vector3 (-28.40545f, -6.06f, -16.32832f)
			};
			
			if (lightFlareList != null) {
				lightFlareChild = new GameObject[lightFlareList.Length];
				for (int i = 0; i < lightFlareList.Length; i++) {
					lightFlareChild[i] = lightFlareList[i].GetComponentInChildren<MeshRenderer>().gameObject;
				}
			}
		}

        #endregion

       
		public override void ChangedMood (Color colorToChange, float speedModifier)
		{
			//We are not changing color in flare in mood change!
			AnimateLightFlare (speedModifier);
		}

		LTDescr colorAnimationOnRing = null;
		LTDescr[] colorAnimationOnFlare = null;
		public override void ChangedBehavior(Color color, float timeModifier)
        {
			if (colorAnimationOnRing != null) {
				LeanTween.cancel(colorAnimationOnRing.uniqueId);
			}
			colorAnimationOnRing = LeanTween.color (gameObject, color, timeModifier); //.setFromColor (Color.white).setLoopPingPong ();

			if (colorAnimationOnFlare != null) {
				for (int i = 0; i < colorAnimationOnFlare.Length; i++) {
					LeanTween.cancel (colorAnimationOnFlare [i].uniqueId);
				}
			} else {
				colorAnimationOnFlare = new LTDescr[3];
			}

			for (int i = 0; i < colorAnimationOnFlare.Length; i++) {
				//LeanTween.va
				GameObject lightFlareObject = lightFlareChild[i];
			
				colorAnimationOnFlare[i] = LeanTween.value (lightFlareChild[i], Color.white, color, timeModifier).setLoopPingPong ().setOnUpdateColor(
					(Color a)=>{
					lightFlareObject.transform.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_TintColor", a);
				});
			}
		}

		LTDescr[] moveAnimationOnFlare = null;
		public void AnimateLightFlare(float timeModifier)
        {
			if (moveAnimationOnFlare != null) {
				for (int i = 0; i < moveAnimationOnFlare.Length; i++) {
					if(moveAnimationOnFlare [i] != null){
						moveAnimationOnFlare [i].setTime(lightFlareAnimationTime * timeModifier);
					}
				}
			} else {
				if(timeModifier > 0.0f){
					moveAnimationOnFlare = new LTDescr[lightFlareList.Length]; 
				}

				if (lightFlareList.Length == listFlarePathList.Length) {
					for (int i = 0; i < lightFlareList.Length; i++) {
						if(timeModifier > 0.0f){
							lightFlareList[i].gameObject.SetActive(true);
							moveAnimationOnFlare [i] = LeanTween.moveLocal (lightFlareList[i], listFlarePathList [i], lightFlareAnimationTime * timeModifier).setOrientToPath(true).setAxis(Vector3.forward).setEase(lightFlareEase).setLoopPingPong ();
						}
						else{
							lightFlareList[i].gameObject.SetActive(false);
						}
					}
				}
			}


		}

		public void StopAnimationLightFlare(){
			for (int i = 0; i < lightFlareList.Length; i++) {
				LeanTween.cancel(lightFlareList[i]);
			}
		}

	}

}