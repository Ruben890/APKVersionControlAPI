# Documentación de la API de Gestión de Archivos APK

![Screenshot 1](./api_image.png)

Esta API está diseñada para manejar y administrar archivos APK, permitiendo a los usuarios obtener información detallada sobre los archivos APK, como la versión, el nombre, y otros metadatos. La API utiliza Java y la librería `AXMLPrinter2.jar` para extraer la información de los archivos APK y presentarla en formato JSON.

## Requisitos Previos

Para que la API funcione correctamente, es necesario tener instalado:

- **Java**: Asegúrate de tener Java 8 o una version superior instalada en tu sistema. Puedes verificar la versión de Java ejecutando el siguiente comando en tu terminal:
  ```bash
  java -version
  ```

- **AXMLPrinter2.jar**: Este archivo ya está incluido en el proyecto dentro de la carpeta `Shared/Lib/AXMLPrinter2.jar`. No es necesario realizar ninguna instalación adicional, pero asegúrate de que la ruta al archivo sea correcta.

## Estructura del Proyecto

El proyecto tiene la siguiente estructura de carpetas:

```
/proyecto
│
├── /Controllers
│   └── APKVersionControlController.cs
│
├── /Extensions
│   ├── BackgroundJobConfigure.cs
│   └── ServicesExtensions.cs
│
├── /Infrastructure
│   ├── /Jobs
│   │   └── ApkFilesJobs.cs
│   └── /Repository
│       └── ApkProcessor.cs
│
├── /Interfaces
│   ├── iRepository.cs
│   ├── iServices.cs
│   └── iBackgroundJob.cs
│
├── /Services
│   └── APKVersionControlServices.cs
│
├── /Shared
│   ├── /Dto
│   │   └── ApkFileDto.cs
│   ├── /Lib
│   │   └── AXMLPrinter2.jar
│   ├── /QueryParameters
│   │   └── GenericParameters.cs
│   └── /Utils
│   │    ├── APKExtractor.cs
│   └── BaseResponses.cs
│
├── /wwwroot
│   └── /Files
│       └── (Archivos APK almacenados)
│
├── appsettings.json
└── README.md
```

## Creación de la Carpeta `wwwroot`

La carpeta `wwwroot` es un directorio esencial en el proyecto, ya que es donde se almacenan los archivos APK que se suben a través de la API.

### ¿Por qué es necesaria la carpeta `wwwroot`?

- **Almacenamiento de Archivos**: La carpeta `wwwroot` actúa como el directorio raíz para el almacenamiento de archivos estáticos, como los archivos APK. Dentro de esta carpeta, se crea una subcarpeta llamada `Files` donde se guardan los archivos APK.
  
- **Organización de Archivos**: Si un archivo APK está asociado a un cliente específico, se crea una subcarpeta dentro de `Files` con el nombre del cliente en minúsculas. Esto permite una organización clara y jerárquica de los archivos.

### Creación Automática de la Carpeta `wwwroot`

La API está diseñada para crear automáticamente la carpeta `wwwroot` y su subcarpeta `Files` al iniciarse por primera vez. Esto se hace mediante un código que verifica la existencia de la carpeta y la crea si no existe. Asegúrate de que la aplicación tenga permisos de escritura en el directorio donde se ejecuta para que este proceso funcione correctamente.

Si la carpeta no se crea automáticamente, puedes crearla manualmente siguiendo estos pasos:

1. Navega a la raíz del proyecto.
2. Crea una carpeta llamada `wwwroot`.
3. Dentro de `wwwroot`, crea una subcarpeta llamada `Files`.

La estructura final debería verse así:

```
/proyecto
│
├── /wwwroot
│   └── /Files
│       └── (Archivos APK almacenados)
│
└── ...
```

## Funcionalidades Principales

1. **Extracción de Metadatos de APK**: La API utiliza `AXMLPrinter2.jar` para extraer información como el nombre, la versión, y otros detalles de los archivos APK.

2. **Presentación de Datos en JSON**: La información extraída se presenta en formato JSON, lo que facilita su consumo por parte de los clientes.

3. **Almacenamiento de Archivos APK**: Los archivos APK se guardan dentro de la carpeta `wwwroot/Files`. Si un archivo APK no tiene un cliente asociado, se guarda directamente en `Files`. Si tiene un cliente, se crea una carpeta dentro de `Files` con el nombre del cliente en minúsculas.

4. **Descarga de Archivos APK**: La API permite descargar un archivo APK específico utilizando el nombre del archivo y la versión.

5. **Eliminación de Archivos APK**: La API permite eliminar un archivo APK específico utilizando el nombre del archivo y la versión.

6. **Búsqueda de Archivos APK**: La API permite buscar archivos APK por nombre, versión y cliente.

7. **Job de Limpieza Automática**: La API incluye un job programado que elimina automáticamente los archivos APK con más de dos meses de antigüedad. Este job se ejecuta periódicamente para mantener el almacenamiento libre de archivos obsoletos.

## Uso de la API

### Insertar un Archivo APK

Para insertar un archivo APK en la API, sigue estos pasos:

1. **Subir el Archivo APK**: Envía el archivo APK a la API mediante una solicitud HTTP POST.
2. **Especificar Parámetros**: Puedes especificar parámetros como `Client`, el cual es un parámetro opcional que representa al cliente al que pertenecerá la APK en la solicitud. Si no se proporciona el parámetro `Client`, la APK no estará asociada a ningún cliente en específico y se guardará de forma general.
3. **Respuesta**: La API devolverá un JSON con un mensaje indicando que se ha guardado el archivo. En caso contrario, lanzará una excepción.

### Obtener Información de un APK

Para obtener información sobre un archivo APK específico, realiza una solicitud HTTP GET con los parámetros `name`, `version`, y `client`.

**Ejemplo de solicitud:**
```http
GET /api/APKVersionControl/GetAllApk?name=apk_mobil&version=18.67&client=x
```

**Respuesta:**
```json
{
    "Name": "apk_mobil",
    "Size": 95.36,
    "Version": "18.67",
    "CreatedAt": "2025-01-23T14:17:40.0229912-04:00",
    "IsCurrentVersion": true,
    "IsPreviousVersion": false,
    "Client": "x"
}
```

### Descargar un Archivo APK

Para descargar un archivo APK específico, realiza una solicitud HTTP GET al endpoint correspondiente con los parámetros `name`, `version`, y `isDownload=true`.

**Ejemplo de solicitud:**
```http
GET /api/APKVersionControl/DownloadApkFile?name=apk_mobil&version=18.67&isDownload=true
```

**Respuesta:**
- Si el archivo existe, se devolverá el archivo APK para su descarga.
- Si el archivo no existe, se devolverá un código de estado 404 (No encontrado).

### Eliminar un Archivo APK

Para eliminar un archivo APK específico, realiza una solicitud HTTP DELETE al endpoint correspondiente con los parámetros `name` y `version`.

**Ejemplo de solicitud:**
```http
DELETE /api/APKVersionControl/DeleteApkFile?name=apk_mobil&version=18.67
```

**Respuesta:**
- Si el archivo existe y se elimina correctamente, se devolverá un mensaje de éxito.
- Si el archivo no existe, se devolverá un código de estado 404 (No encontrado).

### Job de Limpieza Automática

La API incluye un job programado que se ejecuta periódicamente para eliminar archivos APK con más de dos meses de antigüedad. Este job ayuda a mantener el almacenamiento libre de archivos obsoletos.

**Funcionamiento del Job:**
1. **Frecuencia de Ejecución**: El job se ejecuta mensualmente o según la configuración definida en el sistema.
2. **Criterios de Eliminación**: Elimina archivos APK cuya fecha de creación sea mayor a dos meses.
3. **Registro de Actividades**: El job registra las operaciones de eliminación en los logs del sistema para su seguimiento.

## Límite de Subida de Archivos APK

La API tiene un límite de subida de archivos APK de **2 GB**. Si necesitas cambiar este límite, puedes hacerlo modificando la configuración en el archivo de configuración de la API. Para ello, agrega o modifica la siguiente línea en tu configuración:

```csharp
builder.Services.ConfigureMaxRequestBodySize(2L * 1024 * 1024 * 1024); // 2 GB
```

Puedes ajustar el valor según tus necesidades. Por ejemplo, si deseas aumentar el límite a 5 GB, cambia el valor a:

```csharp
builder.Services.ConfigureMaxRequestBodySize(5L * 1024 * 1024 * 1024); // 5 GB
```

## Ejemplo de Respuesta JSON

```json
{
    "Name": "apk_mobil",
    "Size": 95.36,
    "Version": "18.67",
    "CreatedAt": "2025-01-23T14:17:40.0229912-04:00",
    "IsCurrentVersion": true,
    "IsPreviousVersion": false,
    "Client": "x"
}
```

## Configuración

No se requiere configuración adicional más allá de tener Java 8 instalado y asegurarse de que `AXMLPrinter2.jar` esté en la ruta correcta. El job de limpieza automática está configurado para ejecutarse mensualmente según la configuración del sistema.

---