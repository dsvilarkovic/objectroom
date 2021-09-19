# Object Room
Reproduction of the Objects Room dataset from Deep Mind https://github.com/deepmind/multi_object_datasets#objects-room. <br>
The difference between the original repository and ours is that ours includes Depth Maps and reproducible environment. 

The only requirement is Unity (2020.3.7f1 was used, other versions have problems with simultaniously creating depth maps and other masks and rgb image). 

All of the settings like number of spawned objects, types of objects, spawn area etc. are set through the Simulation Manager object in the scene. 
At the moment [Unity Perception SDK](https://github.com/Unity-Technologies/com.unity.perception) doesn't directly render depth maps, so as a workaround this is done through the Recorder. To understand simple changes that could be done on code to generate your properties for the dataset, please check out [tutorial](https://github.com/Unity-Technologies/com.unity.perception/blob/master/com.unity.perception/Documentation~/Tutorial/TUTORIAL.md).

To get RGB + segmentation masks + instance masks - just hit play and all the files will be saved in the c:\Users\XXXX\AppData\LocalLow\ObjectRoom\ObjectRoom\XXXXXX folder

To get RGB + depth + segmentation masks + instance masks - the game needs to be started throug the Recorder window (in case the UI is reset go to Window -> General -> Recorder), adjust settings like output path and start recording. The RGB and depth maps will be saved in the folder set in the Recorder Window settings, the segmentaion and instance masks will be saved the in the Perception output folder, that is c:\Users\XXXX\AppData\LocalLow\ObjectRoom\ObjectRoom\XXXXXX folder


For preprocessing the generated dataset for it to be compatible with resolutions compatible with Clevr and ShapeNet datasets used in [Semi-Supervised Learning of Multi-Object 3D Scene Representations](https://arxiv.org/abs/2010.04030) we run <code>python script.py</code> <br>
Explananation on running the code (setting folder paths for rgb,instance and depth maps) can be found in <code>Unity_recorder_preprocessing.ipynb </code>.<br>
To make everything easier and running properly, folder structure needs to look like this:

```
dataset_folder
│   Unity_recorder_preprocessing.ipynb
│   scripty.py    
│
└───logs #for positions, scales and orientations of the objects
│   │   objects_relative_to_cam.json 
|
└───depth_map # for depth maps of the object
|   │   aov_image_0001.png
|   │   aov_image_0002.png
|   │   aov_image_0003.png
|   │   ...
└───instances # for instance masks of the objects
    │   Instances_1.png
    │   Instances_2.png
    │   Instances_3.png
    │   ...
```
