# Unity to three.js exporter

**Due to changing API's this plugin is currently broken on Unity 5.5\. The last working version is Unity 5.4. Pull requests welcome!**

Export your Unity scenes to the popular JSON format used in Three.js.

[Demo](http://threejsexporter.nickjanssen.com/) - [Changelog](https://docs.google.com/document/d/1H9vKY0JmplnizOQT7eYrC5wk91BsCfSEu_v6pGOdnYk/pub?embedded=true) - [Source code](https://github.com/nickjanssen/ThreeExporter) - [FAQ](https://docs.google.com/document/u/1/d/e/2PACX-1vSUesxdSq9f2ysP3TdDqRrISXwYq9bFK4dGTDPhsmVhk9bjiZHReEnSBzprcz5DvsU02hE2uScqR3wq/pub)

**Supported:**
* Vertices
* UV's
* Normals
* Faces
* Textures
* Materials
* Colliders
* Cameras
* Lights
* Lightmaps
* Script Properties

Requires three.js r71 or higher.

The Three.js Exporter can be found under the Window menu bar.

To load the exported JSON file, use the [THREE.ObjectLoader](http://threejs.org/docs/#Reference/Loaders/ObjectLoader) class. See the demo's source code for an example.