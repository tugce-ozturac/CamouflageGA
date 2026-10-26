
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class DNA : MonoBehaviour
{
 
    public float r { get; private set; }
    public float g { get; private set; }
    public float b { get; private set; }

  
    public float s { get; private set; }

    
    public bool IsDead { get; private set; }
    public float SurvivalTime { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Collider2D hitCollider;

    
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitCollider = GetComponent<Collider2D>();

        originalScale = transform.localScale;
    }

    public void Initialize(
        float red,
        float green,
        float blue,
        float sizeMultiplier = 1f)
    {
     
        r = Mathf.Clamp01(red);
        g = Mathf.Clamp01(green);
        b = Mathf.Clamp01(blue);

       
        s = Mathf.Clamp(sizeMultiplier, 0.5f, 1.5f);

        
        IsDead = false;
        SurvivalTime = 0f;

        
        spriteRenderer.enabled = true;
        hitCollider.enabled = true;

       
        spriteRenderer.color = new Color(r, g, b, 1f);
        transform.localScale = originalScale * s;
    }

    public void Kill(float elapsedTime)
    {

        if (IsDead)
            return;

        IsDead = true;
        SurvivalTime = Mathf.Max(0f, elapsedTime);

        
        spriteRenderer.enabled = false;
        hitCollider.enabled = false;
    }

    public void FinishGeneration(float duration)
    {
        
        if (!IsDead)
        {
            SurvivalTime = duration;
        }
    }
}
