
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PopulationManager : MonoBehaviour
{
    public enum ExperimentMode
    {
        Color,
        Size
    }

    [Header("Experiment")]
    [SerializeField]
    private ExperimentMode experimentMode =
        ExperimentMode.Color;

    [Header("References")]
    [SerializeField] private DNA personPrefab;
    [SerializeField] private Camera sceneCamera;

    [Header("Genetic Algorithm")]
    [SerializeField, Min(2)] private int populationSize = 10;
    [SerializeField, Min(1f)] private float generationDuration = 10f;

    [SerializeField, Range(0.1f, 1f)]
    private float selectedFraction = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float mutationRate = 0.02f;

    [Header("Hunter")]
    [SerializeField] private bool automaticHunter = false;

    [SerializeField] private bool huntBrightestFirst = true;
    [SerializeField] private bool huntLargerFirst = true;

    [SerializeField, Min(0.1f)]
    private float hunterInterval = 1.2f;

    [Header("Reproducibility")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int randomSeed = 12345;

    private readonly List<DNA> population = new List<DNA>();

    private float elapsed;
    private float hunterTimer;
    private int generation = 1;

    private const float FixedColor = 0.5f;
    private const float FixedSize = 1f;

    private struct Genome
    {
        public float r;
        public float g;
        public float b;
        public float s;

        public Genome(float red, float green, float blue, float size)
        {
            r = red;
            g = green;
            b = blue;
            s = size;
        }
    }

    private void Start()
    {
        if (personPrefab == null)
        {
            Debug.LogError("Person Prefab atanmamis!", this);
            enabled = false;
            return;
        }

        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        if (sceneCamera == null)
        {
            Debug.LogError("Main Camera bulunamadi!", this);
            enabled = false;
            return;
        }

        if (useFixedSeed)
        {
            Random.InitState(randomSeed);
        }

        SpawnInitialPopulation();
        LogPopulationStatistics();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        HandleMouseInput();

        if (automaticHunter)
        {
            hunterTimer += Time.deltaTime;

            if (hunterTimer >= hunterInterval)
            {
                hunterTimer = 0f;
                HuntNext();
            }
        }

        if (elapsed >= generationDuration)
        {
            BreedNewPopulation();
        }
    }

    private void SpawnInitialPopulation()
    {
        population.Clear();

        for (int i = 0; i < populationSize; i++)
        {
            if (experimentMode == ExperimentMode.Color)
            {
                
                SpawnPerson(
                    Random.value,
                    Random.value,
                    Random.value,
                    FixedSize
                );
            }
            else
            {
         
                SpawnPerson(
                    FixedColor,
                    FixedColor,
                    FixedColor,
                    Random.Range(0.5f, 1.5f)
                );
            }
        }
    }

    private void SpawnPerson(
        float r,
        float g,
        float b,
        float s)
    {
        Vector3 position = RandomPosition();

        DNA person = Instantiate(
            personPrefab,
            position,
            Quaternion.identity
        );

        person.Initialize(r, g, b, s);
        population.Add(person);
    }

    private Vector3 RandomPosition()
    {
        float halfHeight = sceneCamera.orthographicSize;
        float halfWidth = halfHeight * sceneCamera.aspect;
        float margin = 0.5f;

        float minX = -halfWidth + margin;
        float maxX = halfWidth - margin;
        float minY = -halfHeight + margin;
        float maxY = halfHeight - margin;

        float x = sceneCamera.transform.position.x
                  + Random.Range(minX, maxX);

        float y = sceneCamera.transform.position.y
                  + Random.Range(minY, maxY);

        return new Vector3(x, y, 0f);
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        float distanceToCamera =
            -sceneCamera.transform.position.z;

        Vector3 worldPosition =
            sceneCamera.ScreenToWorldPoint(
                new Vector3(
                    mousePosition.x,
                    mousePosition.y,
                    distanceToCamera
                )
            );

        Collider2D hit = Physics2D.OverlapPoint(
            new Vector2(worldPosition.x, worldPosition.y)
        );

        if (hit != null && hit.TryGetComponent(out DNA target))
        {
            target.Kill(
                Mathf.Min(elapsed, generationDuration)
            );
        }
    }

    private void HuntNext()
    {
        List<DNA> alive = population
            .Where(p => p != null && !p.IsDead)
            .ToList();

        if (alive.Count == 0)
            return;

        DNA target;

        if (experimentMode == ExperimentMode.Color)
        {
            if (huntBrightestFirst)
            {
                target = alive
                    .OrderByDescending(GetBrightness)
                    .First();
            }
            else
            {
                target = alive
                    .OrderBy(GetBrightness)
                    .First();
            }
        }
        else
        {
            if (huntLargerFirst)
            {
                target = alive
                    .OrderByDescending(p => p.s)
                    .First();
            }
            else
            {
                target = alive
                    .OrderBy(p => p.s)
                    .First();
            }
        }

        target.Kill(
            Mathf.Min(elapsed, generationDuration)
        );
    }

    private static float GetBrightness(DNA person)
    {
        return 0.2126f * person.r
             + 0.7152f * person.g
             + 0.0722f * person.b;
    }

    private void BreedNewPopulation()
    {
        foreach (DNA person in population)
        {
            if (person != null)
            {
                person.FinishGeneration(generationDuration);
            }
        }

        List<DNA> ranked = population
            .Where(p => p != null)
            .OrderByDescending(p => p.SurvivalTime)
            .ThenBy(_ => Random.value)
            .ToList();

        if (ranked.Count < 2)
        {
            Debug.LogWarning(
                "Yeni nesil icin en az iki birey gerekli."
            );

            elapsed = 0f;
            hunterTimer = 0f;
            return;
        }

        int k = Mathf.Clamp(
            Mathf.CeilToInt(populationSize * selectedFraction),
            2,
            ranked.Count
        );

        List<Genome> childGenomes = new List<Genome>();

        for (int i = 0; i < populationSize; i++)
        {
            DNA parentA = ranked[Random.Range(0, k)];
            DNA parentB = ranked[Random.Range(0, k)];

            if (experimentMode == ExperimentMode.Color)
            {
                float childR =
                    InheritOrMutate(parentA.r, parentB.r);

                float childG =
                    InheritOrMutate(parentA.g, parentB.g);

                float childB =
                    InheritOrMutate(parentA.b, parentB.b);

                
                childGenomes.Add(
                    new Genome(
                        childR,
                        childG,
                        childB,
                        FixedSize
                    )
                );
            }
            else
            {
                float childS =
                    InheritOrMutate(parentA.s, parentB.s);

              
                childGenomes.Add(
                    new Genome(
                        FixedColor,
                        FixedColor,
                        FixedColor,
                        Mathf.Clamp(childS, 0.5f, 1.5f)
                    )
                );
            }
        }

        foreach (DNA oldPerson in population)
        {
            if (oldPerson != null)
            {
                Destroy(oldPerson.gameObject);
            }
        }

        population.Clear();

        foreach (Genome genes in childGenomes)
        {
            SpawnPerson(
                genes.r,
                genes.g,
                genes.b,
                genes.s
            );
        }

        generation++;
        elapsed = 0f;
        hunterTimer = 0f;

        LogPopulationStatistics();
    }

    private float InheritOrMutate(float parentA, float parentB)
    {
        float inherited =
            Random.value < 0.5f ? parentA : parentB;

        if (Random.value < mutationRate)
        {
            return Random.value;
        }

        return inherited;
    }

    private void LogPopulationStatistics()
    {
        if (population.Count == 0)
            return;

        float meanBrightness =
            population.Average(GetBrightness);

        float meanSize =
            population.Average(p => p.s);

        Debug.Log(
            $"Generation {generation} | " +
            $"Mode: {experimentMode} | " +
            $"Mean Brightness: {meanBrightness:F3} | " +
            $"Mean Size: {meanSize:F3}"
        );
    }
}
