# Neuro Surgical Explorer

Prototipo interactivo de visualización médica desarrollado para el desafío técnico de Navian XR Engineer.

El objetivo fue transformar una MRI volumétrica cerebral en una experiencia clara, explorable e interactiva, combinando visualización 3D, cortes anatómicos, capas segmentadas y herramientas simples de inspección.

> Este proyecto es un prototipo de visualización. No está pensado para diagnóstico médico, planificación quirúrgica real ni navegación intraoperatoria validada.

---

## Qué construí

Construí una escena interactiva en Unity orientada a la exploración neuroquirúrgica conceptual.

La experiencia permite:

- Visualizar una MRI cerebral volumétrica en 3D.
- Activar y desactivar capas anatómicas: MRI Volume, Skin, Gray Matter, White Matter y Veins.
- Modificar la opacidad de las estructuras para reducir carga visual.
- Explorar planos axial, sagital y coronal.
- Mover el plano activo mediante un slider.
- Comparar la vista 3D con una vista 2D del corte MRI.
- Marcar puntos de interés sobre el corte 2D.
- Activar un plano de cross-section para inspeccionar el volumen.
- Cambiar el modo de renderizado entre DVR, MIP e Isosurface.
- Usar una interfaz tipo XR/medical UI desde desktop.

La idea clínica detrás del prototipo es ayudar a entender la relación espacial entre una imagen médica 2D tradicional y su representación volumétrica 3D. No busca resolver una cirugía, sino mostrar cómo una MRI puede convertirse en una experiencia interactiva útil para exploración, comunicación y planificación conceptual.

---

## Cómo ejecutarlo

### Opción 1: desde Unity

1. Clonar el repositorio.
2. Abrir el proyecto con **Unity 6000.4.0f1**.
3. Abrir la escena principal.
Assets/NavianChallenge/Scenes/NavianChallenge_Main.unity

### Opción 2: desde el Build

1. Abrir el proyecto desde el .exe
navian-xr-challenge\Builds\Windows\NavianChallenge.exe

## Decisiones técnicas

## Principales decisiones técnicas

El objetivo fue hacer una experiencia simple, clara y funcional. En lugar de agregar muchas funciones complejas, prioricé que el usuario pueda entender fácilmente qué está viendo y cómo interactuar con la MRI.

La escena se organiza alrededor de cuatro ideas principales:

- Explorar la MRI volumétrica en 3D.
- Activar u ocultar estructuras anatómicas según la necesidad.
- Comparar la vista 3D con cortes 2D de la MRI.
- Inspeccionar el interior del volumen con herramientas simples.

Usé la MRI volumétrica como elemento central del proyecto. Para explorarla, agregué distintos modos de renderizado:

- **DVR:** permite ver el volumen completo.
- **MIP:** resalta intensidades altas del volumen.
- **Isosurface:** muestra una representación más parecida a una superficie.

También separé la visualización por capas anatómicas: piel, sustancia gris, sustancia blanca y venas. Esto permite reducir el ruido visual y enfocarse en estructuras específicas. Por ejemplo, la piel puede servir como referencia externa, mientras que las venas pueden resaltarse como estructuras críticas.

Agregué navegación por planos axial, sagital y coronal porque las imágenes médicas suelen interpretarse en cortes 2D. La idea fue conectar esa forma tradicional de leer una MRI con una visualización espacial en 3D.

El panel **2D Slice Preview** permite comparar el volumen 3D con un corte de la MRI. No busca reemplazar un visor radiológico, sino mostrar cómo una herramienta interactiva puede ayudar a entender mejor la relación entre imagen 2D y anatomía 3D.

También incorporé puntos de interés sobre el corte 2D. Conceptualmente, estos puntos podrían representar una zona a revisar, discutir o localizar dentro del volumen.

Por último, agregué un modo de inspección con cross-section para explorar el interior de la MRI sin perder el contexto general de la anatomía.

## Limitaciones conocidas

- No está validado clínicamente.
- No es una herramienta de diagnóstico.
- No reemplaza un neuronavegador.
- No incluye registro paciente-imagen.
- No tiene tracking intraoperatorio.
- No contempla brain shift, la deformación del cerebro durante cx.
- El punto de interés es exploratorio y no está calibrado clínicamente.
- La vista 2D no es un visor radiológico diagnóstico.
- El dataset corresponde a un atlas, no a un paciente real con patología específica.

Una limitación importante es que no pude validar la solución directamente con un neurocirujano. Me hubiera interesado contrastar si esta visualización realmente ayuda a poner en la balanza qué abordaje tiene menor riesgo, qué estructuras críticas deberían priorizarse y cuál sería el mejor escenario posible cuando todos los caminos implican algún grado de riesgo.

## Qué mejoraría con más tiempo

Con más tiempo, agregaría una ficha médica o historia clínica del caso, incluyendo datos como diagnóstico, localización de la lesión, objetivo quirúrgico y riesgos anatómicos relevantes. Esto permitiría que la herramienta no sea solo un visualizador anatómico, sino una experiencia centrada en un paciente o caso específico.

Otra mejora importante sería agregar una herramienta para comparar posibles rutas de navegación hacia un punto de interés, por ejemplo un tumor. La idea no sería que el software decida la cirugía, sino que ayude a evaluar qué camino podría implicar menor daño o menor complejidad, considerando profundidad, cercanía a venas, estructuras críticas y riesgo relativo.


También agregaría mediciones, como distancia desde la superficie al target, distancia entre el target y estructuras críticas, y profundidad aproximada.

Por último, adaptaría la visualización según el usuario: una vista más completa para el cirujano, una vista de briefing para el equipo quirúrgico o instrumentador, y una vista simplificada para explicar el caso al paciente de forma guiada y sin generar ansiedad innecesaria.


## Estructura final del proyecto

navian-xr-challenge/
├── README.md
├── .gitignore
│
├── Assets/
│   ├── NavianChallenge/              ← TODO lo específico del desafío
│   │   ├── Scenes/
│   │   │   └── NavianChallenge_Main.unity     ← ESCENA PRINCIPAL
│   │   ├── Data/Atlas/IXI025/
│   │   │   ├── MRI/IXI025_t1.nii.gz            ← MRI T1 (se auto-importa como VolumeDataset)
│   │   │   └── Meshes/                          ← los 4 meshes 3D (.obj)
│   │   │       ├── SustanciaGris.obj  SustanciaBlanca.obj  Venas.obj  Piel.obj
│   │   ├── Materials/                 ← 1 material por estructura (editables)
│   │   ├── Shaders/
│   │   │   └── ClippedTransparent.shader   ← shader de las 4 mallas (transparente + corta junto al MRI)
│   │   ├── Scripts/                   ← helpers de runtime (base, mínimos)
│   │   │   ├── AtlasVolumeLoader.cs        ← carga/crea la MRI (edición + Play)
│   │   │   ├── AtlasSceneController.cs     ← cámara orbital + reset + ayuda
│   │   │   └── UI/                         ← panel "Neuro Surgical Explorer" (construido en runtime)
│   │   │       ├── ChallengeUIBootstrap.cs       ← lo crea al entrar en Play, sin tocar la escena
│   │   │       ├── ChallengeHUD.cs               ← composition root: arma Canvas + las 6 secciones
│   │   │       ├── UIFactory.cs                  ← fábrica de controles procedurales (botón/slider/toggle/…)
│   │   │       ├── StructureVisibilityController.cs  ← Anatomy Layers (show/hide + opacidad)
│   │   │       ├── StructureSelector.cs          ← click-to-select + panel de info por estructura
│   │   │       ├── SliceExplorerController.cs    ← Anatomical Planes + 2D Slice Preview
│   │   │       ├── CrossSectionController.cs     ← Volume Inspection (cross-section del MRI + mallas)
│   │   │       ├── PointOfInterestController.cs  ← Point of Interest (marcadores 3D desde el corte 2D)
│   │   │       ├── TransferFunctionController.cs ← Render Mode (DVR/MIP/Isosurface + ventana + luz)
│   │   │       └── ClinicalPresetsController.cs  ← sin usar hoy (ver §0, fácil de reactivar)
│   │   └── Editor/                    ← herramientas de editor (opcionales)
│   │
│   └── ThirdParty/                    ← librerías de terceros
│       ├── UnityVolumeRendering/      ← volume rendering (código fuente: Scripts, Editor,
│       │                                Shaders, Materials, Resources)
│       ├── Nifti.NET/                 ← lector NIfTI managed (soporta .nii y .nii.gz)
│       └── openDicom/                 ← dependencia DICOM de UVR
│
└── docs/images/                      ← imagen usada en este README
```