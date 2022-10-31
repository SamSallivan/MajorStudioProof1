using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
public class PlayerAudio : MonoBehaviour
{
    [SerializeField]
    public EmitterRef PlayerSlide;
    [SerializeField] private EmitterRef PlayerBGM;
    [SerializeField] private EmitterRef GroundHit;
    public StudioEventEmitter test;
    // ---reference values--
    public float Rvalue;        //slide rotation, left to right: -90 - 90
    public float Hvalue;        //slide hardness, light to hard: 0-1
    public float Heightvalue;   //current space, on ground to in air: 0-1
    public float Hitvalue;      //how hard player hit floor, bad to perfect landing: 0-1
    FMOD.Studio.EventInstance playerState;
    FMOD.Studio.PARAMETER_ID fullHealthParameterId;

    [FMODUnity.EventRef]
    public string HealEvent = "";
    private void PlayEmitterAndSetParameter()
    {
        //anEmitter.Target.Play();
        PlayerSlide.Target.SetParameter("SlideRotation", Rvalue);
        PlayerSlide.Target.SetParameter("SlideHardness", Hvalue);
        PlayerBGM.Target.SetParameter("Space", Heightvalue);
    }

    private void Start()
    {
        
        FMOD.Studio.EventDescription fullHealEventDescription = FMODUnity.RuntimeManager.GetEventDescription(HealEvent);
        FMOD.Studio.PARAMETER_DESCRIPTION fullHealParameterDescription;
        fullHealEventDescription.getParameterDescriptionByName("HitValue", out fullHealParameterDescription);
        fullHealthParameterId = fullHealParameterDescription.id;
    }
    private void StopEmitter()
    {
        PlayerSlide.Target.Stop();
        PlayerBGM.Target.Stop(); 
    }

    // Update is called once per frame
    void Update()
    {
        PlayEmitterAndSetParameter();
        if (Input.GetKeyDown(KeyCode.P))
        {
            playerState = FMODUnity.RuntimeManager.CreateInstance("event:/GroundHit");
            
            playerState.setParameterByID(fullHealthParameterId, Hitvalue);
            playerState.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
            playerState.start();
            //playerState.release();
            //GroundHit.Target.Play();
            //test.Play();
        }
    }
}
