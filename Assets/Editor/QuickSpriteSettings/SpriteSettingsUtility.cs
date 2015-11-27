﻿using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace Staple.EditorScripts
{
    public class SpriteSettingsUtility
    {
        public static void ApplyDefaultTextureSettings(
            SpriteSettings prefs, SpriteSlicingOptions slicingOptions,
            bool changePivot,
            bool changePackingTag)
        {
            if (prefs == null) return;

            foreach (var obj in Selection.objects)
            {
                if (!AssetDatabase.Contains(obj)) continue;

                string path = AssetDatabase.GetAssetPath(obj);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                
                // When we have text file data
                if (slicingOptions.CellSize != Vector2.zero)
                {
                    SpriteMetaData[] spriteSheet;
                    if (slicingOptions.SlicingMode == SpriteSlicingOptions.GridSlicingMode.SliceAll) {
                        spriteSheet = SpriteSlicer.CreateSpriteSheetForTexture (AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D,
                            slicingOptions.CellSize, prefs.Pivot);
                    } else {
                        spriteSheet = SpriteSlicer.CreateSpriteSheetForTextureBogdan (AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D,
                            slicingOptions.CellSize, changePivot, prefs.Pivot, prefs.CustomPivot, (uint)slicingOptions.Frames);
                    }

                    // If we don't do this it won't update the new sprite meta data
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteImportMode = SpriteImportMode.Multiple;

                    importer.spritesheet = spriteSheet;
                }
                else if (importer.spritesheet != null && changePivot) // for existing sliced sheets without data in the text file and wantint to change pivot
                {
                    var spriteSheet = new SpriteMetaData[importer.spritesheet.Length];
                    
                    for (int i = 0; i < importer.spritesheet.Length; i++)
                    {
                        var spriteMetaData = importer.spritesheet[i];
                        spriteMetaData.alignment = (int)prefs.Pivot;
                        spriteMetaData.pivot = prefs.CustomPivot;
                        spriteSheet[i] = spriteMetaData;
                    }
                    
                    importer.spritesheet = spriteSheet;
                }
                else
                    importer.spriteImportMode = SpriteImportMode.Single;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                settings.filterMode = prefs.FilterMode;
                settings.wrapMode = prefs.WrapMode;
                settings.mipmapEnabled = prefs.GenerateMipMaps;
                settings.textureFormat = prefs.TextureFormat;
                settings.maxTextureSize = prefs.MaxSize;

                settings.spritePixelsPerUnit = prefs.PixelsPerUnit;

                settings.spriteExtrude = (uint)Mathf.Clamp(prefs.ExtrudeEdges, 0, 32);
                settings.spriteMeshType = prefs.SpriteMeshType;

                if (changePivot)
                {
                    settings.spriteAlignment = (int)prefs.Pivot;
                    if (prefs.Pivot == SpriteAlignment.Custom)
                        settings.spritePivot = prefs.CustomPivot;
                }

                if (changePackingTag)
                    importer.spritePackingTag = prefs.PackingTag;

                importer.SetTextureSettings(settings);
#if UNITY_5_0
                importer.SaveAndReimport();
#else
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
#endif
                EditorUtility.SetDirty(obj);
            }
        }


        public static SpriteSlicingOptions GetSlicingOptions(string path, string dataFileName)
        {
            var spriteSheetDataFile = AssetDatabase.LoadAssetAtPath(
                Path.GetDirectoryName(path) + "/" + dataFileName, typeof(TextAsset)
                ) as TextAsset;

            return GetSlicingOptions(path, spriteSheetDataFile);
        }

        public static SpriteSlicingOptions GetSlicingOptions(string path, TextAsset slicingOptionsDataFile)
        {
            if (slicingOptionsDataFile != null)
            {
                string[] entries = slicingOptionsDataFile.text.Split(
                    new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

                string entry = entries.FirstOrDefault(x => x.StartsWith(Path.GetFileName(path)));

                if (!string.IsNullOrEmpty(entry))
                {
                    try {
                        // Strip filename
                        int firstIndex = entry.IndexOf (',') + 1;
                        int lastIndex = entry.Length - 1;
                        var slicingData = entry.Substring (firstIndex, lastIndex - firstIndex);
                        return SpriteSlicingOptions.FromString (slicingData);
                    }
                    catch
                    {
                        Debug.LogError("Invalid sprite data at line: " + Array.IndexOf(entries, entry) + ", (" + entry + ")");
                    }
                }
            }

            return new SpriteSlicingOptions ();
        }
    }
}