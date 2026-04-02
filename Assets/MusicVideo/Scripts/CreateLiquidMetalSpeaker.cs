using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateLiquidMetalSpeaker : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject prefab;
    public GameObject parent;
    public int colorHue;
    public int size = 25; // Size of the grid (number of shapes in one direction)

    int rowCount, colCount;
    GameObject[][] shapes;
    Renderer[][] shapesRenderers;
    Quaternion targetRotation;

    [Header("Glow Trail")]
    public Material trailMaterial;
    public Material shapeMaterial;
    public float trailTime = 1.2f;

    [Header("Audio")]
    // Global visual controls for all three plots.
    public float scale = 10f;
    public int displayBins = 192;
    public float plotWidth = 16f;
    public float rowGap = 2.8f;
    public Vector3 origin = new Vector3(-8f, 4f, 0f);
    // Lowest frequency used for the log-scale mapping.
    public float minLogHz = 20f;
    // Number of representative x positions to annotate.
    public int labelCount = 8;
    // Negative value moves labels below the x-axis.
    public float labelYOffset = -0.45f;
    private const int RowCount = 3;
    private const int LinearRow = 0;
    private const int LogRow = 1;
    private const int MelRow = 2;

    int[,] mappedBins;
    float[,] mappedHz;
    int[] labelDisplayIndices;

    int fftSize;
    float hzPerBin;
    float nyquist;
    // float xStep;
    bool speakerFormed = false;

    // Start is called before the first frame update
    void Start() 
    {
        targetRotation = Quaternion.identity;
        InitShapes();
        SetupAudioReactivity();
    }    

    void Update() 
    {
        if(speakerFormed)
        {
            AnimateSpeaker();
        }
    }

    void InitShapes()
    {
        rowCount = 2 * size + 1;
        colCount = (int) ((3.25f) * size + 1);

        shapes = new GameObject[rowCount][];
        shapesRenderers = new Renderer[rowCount][];

        for(int row = 0; row < rowCount; row++)
        {
            shapes[row] = new GameObject[colCount];
            shapesRenderers[row] = new Renderer[colCount];

            for (int col = 0; col < colCount; col++)
            {    
                shapes[row][col] = GameObject.Instantiate(prefab);     

                int x = col - (colCount / 2); // Center the shapes around (0, 0)
                int y = row - (rowCount / 2); // Center the shapes around (0, 0)
                float radius = Mathf.Sqrt(x * x + y * y);   // Fun fact Pythagorous wasnt even the first to discover this thats just white supremacy type.

                float scaleOfShape = 1.5f*(Mathf.Sin(radius * (2*Mathf.PI / ((float)size * 2)) - (Mathf.PI/4)) ) + 1.5f; // Scale shapes based on radius (optional)
                
                float colorHue = 1*(Mathf.Sin(radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) +1f;
                
                
                shapes[row][col].transform.position = new Vector3(x, y, 10f);
                shapes[row][col].transform.localScale = new Vector3(scaleOfShape * .3f,scaleOfShape * .3f,.05f); 
                shapes[row][col].transform.parent = parent.transform;

                shapesRenderers[row][col] = shapes[row][col].GetComponent<Renderer>();
                shapesRenderers[row][col].material = shapeMaterial;
                shapesRenderers[row][col].material.color = Color.HSVToRGB(colorHue, 1f, 1f); // Full saturation and brightness
            }
        }
    }


    void AnimateSpeaker() 
    {
        for(int row = 0; row < rowCount; row++) 
        {
            for(int col = 0; col < colCount; col++) 
            {
                int x = col - (colCount / 2); // Center the shapes around (0, 0)
                int y = row - (rowCount / 2); // Center the shapes around (0, 0)
                float z = 10f;

                float radius = Mathf.Sqrt(x * x + y * y); 
            
                if (AudioSpectrum.samples != null) 
                {
                    int bin = ((int)radius > displayBins - 1 )? 0 : (displayBins - 1) - (int)radius;                   
                    int fftBin = mappedBins[MelRow, bin];
                    float amp = Mathf.Max(AudioSpectrum.samples[fftBin] * scale * scale, 0.001f);
                    z = 10f - (amp); // Move shape based on amplitude (optional)
                } 

                float scaleOfRotation = 15 * (
                    Mathf.Sin(Time.time + radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1
                ) + 1.5f; // Scale shapes based on radius (optional)

                float colorHue = (radius/size) * 1*(Mathf.Sin(Time.time + radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) +1f;

                shapes[row][col].transform.position = new Vector3(x, y, z); // Move shape based on amplitude (optional)
                shapes[row][col].transform.rotation = Quaternion.Euler(Time.time * scaleOfRotation, 0, Time.time * scaleOfRotation); // Rotate shapes over time
                
                // shapesRenderers[row][col].material.color = Color.HSVToRGB(colorHue, 1f, 1f); // Full saturation and brightness

                // if(!speakerFormed2) 
                // {
                //     colorHue = (radius/size) * Mathf.Sin( Mathf.PI/2 * Time.time ) + .05f;
                // } else {
                //     colorHue = 1*(Mathf.Sin(Time.time + radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) +1f;
                // }

                //  colors[row][col][2] = Color.HSVToRGB(colorHue, 100f, colorHue); // Full saturation and brightness
                //  shapesRenderers[row][col].material.color = colors[row][col][2];
                // }
            }
        }
    }


    void SetupAudioReactivity()
    {
        // FFT settings come from AudioSpectrum (source of spectrum data).
        fftSize = AudioSpectrum.FFTSIZE;

        // Nyquist is the maximum analyzable frequency in digital audio.
        nyquist = AudioSettings.outputSampleRate * 0.5f;
        hzPerBin = nyquist / fftSize;
        displayBins =  (int) (size * 2);

        // Keep settings in a practical range for the classroom demo.
        displayBins = Mathf.Clamp(displayBins, 16, fftSize);
        // labelCount = Mathf.Clamp(labelCount, 2, displayBins);
        // xStep = plotWidth / (displayBins - 1);
    
        mappedBins = new int[RowCount, displayBins];
        mappedHz = new float[RowCount, displayBins];
        // labelDisplayIndices = BuildRepresentativeIndices(labelCount, displayBins);

        // 1) Map display x positions to FFT bins for each scale.
        BuildMappings();
        speakerFormed = true;
    }


    void BuildMappings()
    {
        // Build each row separately so students can read one scale at a time.
        BuildLinearMapping();
        BuildLogMapping();
        BuildMelMapping();
    }

    void BuildLinearMapping()
    {
        for (int i = 0; i < displayBins; i++)
        {
            // t is x position in [0, 1].
            float t = NormalizedX(i);
            float hz = t * nyquist;
            SetMapping(LinearRow, i, hz);
        }
    }

    void BuildLogMapping()
    {
        float safeMinHz = Mathf.Clamp(minLogHz, hzPerBin, nyquist);

        for (int i = 0; i < displayBins; i++)
        {
            float t = NormalizedX(i);
            float hz = 0f;

            if (t > 0f)
            {
                // Convert [0,1] -> log-frequency range.
                hz = Log01ToHz(t, safeMinHz, nyquist);
            }

            SetMapping(LogRow, i, hz);
        }
    }

    void BuildMelMapping()
    {
        float melMax = HzToMel(nyquist);

        for (int i = 0; i < displayBins; i++)
        {
            float t = NormalizedX(i);
            float hz = 0f;

            if (t > 0f)
            {
                // Interpolate in mel space, then convert mel -> Hz.
                float melValue = Mathf.Lerp(0f, melMax, t);
                hz = MelToHz(melValue);
            }
            SetMapping(MelRow, i, hz);
        }
    }

    float NormalizedX(int index)
    {
        return index / (float)(displayBins - 1);
    }

    float Log01ToHz(float t, float minHz, float maxHz)
    {
        float minLog10 = Mathf.Log10(minHz);
        float maxLog10 = Mathf.Log10(maxHz);
        float logValue = Mathf.Lerp(minLog10, maxLog10, t);
        return Mathf.Pow(10f, logValue);
    }

    void SetMapping(int row, int displayIndex, float hz)
    {
        // Convert continuous frequency (Hz) to a discrete FFT bin index.
        int fftBin = Mathf.Clamp(Mathf.RoundToInt(hz / hzPerBin), 0, fftSize - 1);
        mappedBins[row, displayIndex] = fftBin;
        // Store quantized frequency (actual bin center used in drawing).
        mappedHz[row, displayIndex] = fftBin * hzPerBin;
    }

    // static int[] BuildRepresentativeIndices(int count, int size)
    // {
    //     // Evenly distribute label positions across the displayed bins.
    //     int[] indices = new int[count];
    //     for (int i = 0; i < count; i++)
    //     {
    //         indices[i] = Mathf.RoundToInt(i * (size - 1) / (float)(count - 1));
    //     }
    //     return indices;
    // }

    static float HzToMel(float hz)
    {
        // Standard mel conversion (Stevens, Volkmann, Newman approximation).
        return 2595f * Mathf.Log10(1f + hz / 700f);
    }

    static float MelToHz(float mel)
    {
        // Inverse of HzToMel.
        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1f);
    }
}
