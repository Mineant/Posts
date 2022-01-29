using System;
using System.Collections;
using UnityEngine;



public class SimpleAnimationSpeedModifier : MonoBehaviour
{
    [Serializable]
    public struct AnimationSettings
    {
        [Tooltip("Same as the animation clip you are going to trigger at the animator.Used for getting the framerate, length of the animation you are going to play.I just dont want to deal with getting animation clips from animator at runtime, seems to much hassle")]
        /// <summary>
        /// Same as the animation clip you are going to trigger at the animator.
        /// Used for getting the framerate, length of the animation you are going to play.
        /// I just dont want to deal with getting animation clips from animator at runtime, seems to much hassle
        /// </summary>
        public AnimationClip clip;

        [Tooltip("0 = non affected by the slowness of the animation. A higher value means that part of the animation is going to be affected by the slowness more. Usually 1 is the highest, it really doesn't matter which is the highest value.")]
        /// <summary>
        /// 0 = non affected by the slowness of the animation
        /// A higher value means that part of the animation is going to be affected by the slowness more. Usually 1 is the highest, it really doesn't matter which is the highest value.
        /// </summary>
        public AnimationCurve curve;

        [Tooltip("The trigger to set to play the desired animation at animator.")]

        /// <summary>
        /// The trigger to set to play the desired animation at animator.
        /// </summary>
        public string trigger;
    }
    [Tooltip("The target animator to play")]
    public Animator animator;  // Animator to play
    [Tooltip("How much to modify. The higher the slower")]
    [Range(1.0f, 10.0f)] public float modifyPercent = 1f;   // How much to modify. The higher the slower

    public AnimationSettings settings;

    private const string speedFloat = "speed";  // change this if you have another speed parameter to controll speed of animation
    float[] _curveValues; // Use startIgnoreFrame, endIgnoreFrame to build array to mimic curve
    float _animTime;    // Duration in seconds of animation clip
    float _newAnimTime; // New Duration in seconds of animation clip
    int _animFrames;    // Total frame in original animation clip
    float _timeBetweenEachFrame;    // original time between each frame
    float[] _newTimeBetweenEachFrame;   // New time between each frame for each adn every frame in the animation clip.
    int _animFPS;   // fps of animation clip
    Vector3 _initialPosition;   // initial position of character to test (used if you have root motion applied)
    Coroutine _animationRoutine;


    void Start()
    {
        _initialPosition = animator.gameObject.transform.position;
    }
    public void ResetCharacterPosition()
    {
        animator.transform.position = _initialPosition;
    }

    /// <summary>
    /// Calculates the new time between each frame.
    /// Say the original Clip is a 1s clip with 30fps. The time between each frame will be 0.0333.
    /// If i slowed down the animation by half speed, the new time between will be 0.333 * 2 = 0.666.
    /// The effect here is some parts of the animation i don't want it to be affected by the slowness
    /// lets say when a sword swings, I want to keep the time between each frame to be 0.333.
    /// While I want to distribute the slowness more to other parts, like the anticipation for the swing, 
    /// so the new time between each frame for that part will be even higher than the average new time between 
    /// of 0.0666, this one will maybe at 0.1s, to compensate for the time we didn't slowed down for the swing part.
    /// This calculates the new time between based on weights, so it doesn't matter whether if the highest and lowest value 
    /// in the animation curve is 1 and 0 or 1000 and 0, as long as the curve looks the same.
    /// </summary> 
    public void CalculateCurveToAnimation(AnimationClip targetAnimClip, AnimationCurve targetCurve)
    {
        // Set base values of animation clip
        _animTime = targetAnimClip.length;
        _animFPS = (int)Mathf.Round(targetAnimClip.frameRate);
        _animFrames = (int)Mathf.Round(_animTime * _animFPS);
        _timeBetweenEachFrame = (float)1 / (float)_animFPS;
        _newAnimTime = _animTime * modifyPercent;

        // Get Animation Curve Values
        _curveValues = new float[_animFrames - 1];
        float curveLength = targetCurve[targetCurve.length - 1].time;
        float timeBetweenCurveKey = curveLength / _animFrames;
        for (int i = 0; i < _animFrames - 1; i++)
        {
            _curveValues[i] = targetCurve.Evaluate(timeBetweenCurveKey * i);
        }

        // Calculate total weight
        float totalWeight = 0;
        for (int i = 0; i < _animFrames - 1; i++)
        {
            // Add all weight of frames to total
            totalWeight += _curveValues[i];
        }

        // Calculate normals for each frame
        _newTimeBetweenEachFrame = new float[_animFrames - 1];
        for (int i = 0; i < _animFrames - 1; i++)
        {
            // Distribute normalized weight
            float weight = _curveValues[i] / totalWeight;
            _newTimeBetweenEachFrame[i] = _timeBetweenEachFrame + (_newAnimTime - _animTime) * weight;
        }
    }
    private void ResetRoutines()
    {
        if (_animationRoutine != null) StopCoroutine(_animationRoutine);
    }

    /// <summary>
    /// Plays the animation at normal speed
    /// </summary>  
    public void PlayAnimation()
    {
        ResetRoutines();
        animator.SetFloat(speedFloat, 1);
        animator.SetTrigger(settings.trigger);
    }

    /// <summary>
    /// Plays the animation at modified speed
    /// </summary> 
    public void PlayModifiedAnimation()
    {
        ResetRoutines();
        animator.SetFloat(speedFloat, 1 / modifyPercent);
        animator.SetTrigger(settings.trigger);
    }

    /// <summary>
    /// Plays the animation at the adjuested modified speed
    /// </summary> 
    public void PlayAdjustedModifiedAnimation()
    {
        ResetRoutines();
        CalculateCurveToAnimation(settings.clip, settings.curve);
        animator.SetTrigger(settings.trigger);
        _animationRoutine = StartCoroutine(PlayAnimationCoroutine());
        StartCoroutine(CountdownRoutine(_newAnimTime, "Original time to finish"));
    }

    /// <summary>
    /// Adjust the speed of the animation while the animation plays.
    /// Your state to be played should have a speed modifier called "speed", as this uses
    /// the speed multiplier of a state to modify its speed. 
    /// </summary> 
    IEnumerator PlayAnimationCoroutine()
    {
        float startTime = Time.time;
        for (int i = 0; i < _animFrames - 1; i++)
        {
            animator.SetFloat(speedFloat, _timeBetweenEachFrame / _newTimeBetweenEachFrame[i]);
            if (i == _animFrames - 1 - 1) break;
            yield return new WaitForSeconds(_newTimeBetweenEachFrame[i]);
        }
        float finishTime = Time.time - startTime;
        print($"New animation finished with {finishTime} seconds");
    }

    /// <summary>
    /// Used for counting down the original modified time.
    /// </summary> 
    IEnumerator CountdownRoutine(float time, string message)
    {
        float startTime = Time.time;
        yield return new WaitForSeconds(time);
        float finishTime = Time.time - startTime;
        print($"{message}. Finished Countdown by {finishTime} seconds. ");
    }

}
