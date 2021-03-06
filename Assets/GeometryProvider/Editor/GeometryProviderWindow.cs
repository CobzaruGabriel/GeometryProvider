﻿using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace TheGoldenMule.Geo.Editor
{

    public class GeometryProviderWindow : EditorWindow
    {
        private static readonly Material DefaultMaterial;

        static GeometryProviderWindow()
        {
            DefaultMaterial = new Material(Shader.Find("Diffuse"))
            {
                mainTexture = GenerateTestTexture()
            
            };
        }

        private static Texture2D GenerateTestTexture()
        {
            const int DIM = 128;
            const int LEN = DIM * DIM;
            const int PATTERN_DIM = 16;

            var checkerboard = new Texture2D(DIM, DIM, TextureFormat.RGBA32, true, true);

            var colors = new Color[LEN];
            for (var x = 0; x < DIM; x++)
            {
                for (var y = 0; y < DIM; y++)
                {
                    var index = x + y * DIM;

                    if (0 == (x / PATTERN_DIM) % 2)
                    {
                        if (1 == (y / PATTERN_DIM) % 2)
                        {
                            colors[index] = Color.white;
                        }
                        else
                        {
                            colors[index] = Color.black;
                        }
                    }
                    else
                    {
                        if (1 == (y / PATTERN_DIM) % 2)
                        {
                            colors[index] = Color.black;
                        }
                        else
                        {
                            colors[index] = Color.white;
                        }
                    }
                }
            }

            checkerboard.SetPixels(colors);
            checkerboard.Apply(true);

            return checkerboard;
        }

        private readonly List<string> _builderNames = new List<string>();
        private readonly List<IGeometryBuilderRenderer> _renderers = new List<IGeometryBuilderRenderer>();

        private int _selectedBuilderIndex = -1;
        private GeometryBuilderSettings _settings;
        private IGeometryBuilder _builder;
        private IGeometryBuilderRenderer _renderer;
        private GameObject _gameObject;
        private Mesh _mesh;

        private void OnEnable()
        {
            minSize = new Vector2(168, 300);
            title = "Geo Editor";

            foreach (var builder in GeometryProvider.Builders)
            {
                _builderNames.Add(Name(builder));

                var renderer = Renderer(builder);
                renderer.OnCreate += OnCreatePrimitive;
                renderer.OnUpdate += OnUpdatePrimitive;
                _renderers.Add(renderer);
            }

        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Primitive");
            var index = Mathf.Max(EditorGUILayout.Popup(_selectedBuilderIndex, _builderNames.ToArray()), 0);
            GUILayout.EndHorizontal();

            if (index >= 0 && _renderers.Count > index)
            {
                if (index != _selectedBuilderIndex || null == _renderer)
                {
                    _settings = (GeometryBuilderSettings) GeometryProvider.Factories[index].Invoke(null, null);

                    _renderer = _renderers[index];
                    _builder = GeometryProvider.Builders[index];

                    _settings.Name = Name(_builder);
                    _settings.Description = Description(_builder);
                }

                _renderer.Draw(_settings);

                _selectedBuilderIndex = index;
            }

            GUILayout.EndVertical();
        }

        private void OnCreatePrimitive()
        {
            _gameObject = CreateGameObject("Primitive");
            _mesh = new Mesh();

            _gameObject.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _gameObject.GetComponent<MeshRenderer>().sharedMaterial = DefaultMaterial;

            // select the new primitive
            EditorUtility.SelectAndFocus(_gameObject.transform);
            if (null != _renderer)
            {
                _renderer.Selected = _gameObject.transform;
            }

            OnUpdatePrimitive();
        }

        private void OnUpdatePrimitive()
        {
            GeometryProvider.Build(_mesh, _builder, _settings);
        }

        private static GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);

            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = DefaultMaterial;

            return gameObject;
        }

        private static string Name(IGeometryBuilder builder)
        {
            var builderType = builder.GetType();
            var customName = builderType.Attribute<DisplayNameAttribute>();
            if (null != customName)
            {
                return customName.DisplayName;
            }

            return builderType.Name;
        }

        private static string Description(IGeometryBuilder builder)
        {
            var builderType = builder.GetType();
            var customName = builderType.Attribute<DescriptionAttribute>();
            if (null != customName)
            {
                return customName.Description;
            }

            return "[No description]";
        }

        private static IGeometryBuilderRenderer Renderer(IGeometryBuilder builder)
        {
            var builderType = builder.GetType();
            var rendererTypes = typeof(IGeometryBuilderRenderer).Implementors();

            foreach (var rendererType in rendererTypes)
            {
                var customRenderer = rendererType.Attribute<CustomRenderer>();
                if (null == customRenderer)
                {
                    continue;
                }

                if (customRenderer.Type == builderType)
                {
                    try
                    {
                        var customRendererInstance = (IGeometryBuilderRenderer) Activator.CreateInstance(rendererType);
                        return customRendererInstance;
                    }
                    catch
                    {
                        Debug.LogError(string.Format(
                            "Could not instantiate custom IGeometryBuilderRenderer {0}. Does it have a public default constructor?",
                            rendererType));
                        break;
                    }
                }
            }

            return new StandardGeometryBuilderRenderer();
        }

        [MenuItem("GameObject/Geometry Editor %g")]
        private static void Toggle()
        {
            var window = FindObjectOfType<GeometryProviderWindow>();
            if (null != window)
            {
                window.Close();
            }
            else
            {
                GetWindow<GeometryProviderWindow>();
            }
        }
    }
}