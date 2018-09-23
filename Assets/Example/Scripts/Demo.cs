using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TilemapGenerator.Behaviours;

public class Demo : MonoBehaviour
{
    public LandGenerator Generator;
    public Text ScaleValue;
    public Slider ScaleSlider;
    public Text HeightValue;
    public Slider HeightSlider;
    public Text Seed;

    private void Start()
    {
        ScaleSlider.maxValue = Mathf.Max(Generator.NoiseScale * 2f, 20f);
        ScaleSlider.value = Generator.NoiseScale;
        HeightSlider.maxValue = ScaleSlider.value / 3f;
        HeightSlider.value = Generator.Height;
        SetValues();
    }

    private void SetValues()
    {
        ScaleValue.text = Generator.NoiseScale.ToString();
        HeightValue.text = Generator.Height.ToString();
        Seed.text = "Seed: #" + Generator.Seed.ToString();
    }

    public void ValueChanged()
    {
        Generator.NoiseScale = Mathf.Max(ScaleSlider.value, 20f);
        ScaleSlider.maxValue = Mathf.Max(Generator.NoiseScale * 2f, 20f);
        HeightSlider.maxValue = ScaleSlider.value / 3f;
        Generator.Height = Mathf.RoundToInt(HeightSlider.value);
        SetValues();
        Generator.Generate();
    }

    public void GenerateRandom()
    {
        Generator.Seed = Random.Range(-999999, 999999);
        Seed.text = "Seed: #" + Generator.Seed.ToString();
        Generator.Generate();
    }
}
