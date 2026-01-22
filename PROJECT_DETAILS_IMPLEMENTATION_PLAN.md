# План реализации: Детали проекта с настройками FSRS

## 📋 Обзор

Реализация функционала для просмотра и редактирования деталей проекта, включая настройки алгоритма FSRS (SR-STR-02).

---

## 🔧 Бэкенд (Backend)

### 1. VocabularyService (gRPC сервис)

#### 1.1. ProjectService - добавить методы

**Файл:** `Polyraspad/VocabularyService/Services/ProjectService.cs`

**Добавить методы:**

```csharp
/// <summary>
/// Получает детали проекта по идентификатору
/// </summary>
public async Task<Project?> GetProjectByIdAsync(
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    var project = await _context.Projects
        .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    if (project == null)
    {
        return null;
    }

    // Проверка прав доступа: проект должен принадлежать пользователю
    if (project.UserId != userId)
    {
        throw new UnauthorizedAccessException("Project does not belong to user");
    }

    return project;
}

/// <summary>
/// Обновляет проект
/// </summary>
public async Task<Project> UpdateProjectAsync(
    Guid projectId,
    Guid userId,
    string? title = null,
    bool? isArchived = null,
    JsonTypes.FsrsSettings? fsrsSettings = null,
    CancellationToken cancellationToken = default)
{
    var project = await GetProjectByIdAsync(projectId, userId, cancellationToken);
    
    if (project == null)
    {
        throw new KeyNotFoundException($"Project {projectId} not found");
    }

    // Обновляем только переданные поля
    if (title != null)
    {
        project.Title = title;
    }

    if (isArchived.HasValue)
    {
        project.IsArchived = isArchived.Value;
    }

    if (fsrsSettings != null)
    {
        // Валидация настроек FSRS
        ValidateFsrsSettings(fsrsSettings);
        project.FsrsSettings = fsrsSettings;
    }

    project.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync(cancellationToken);

    _logger.LogInformation(
        "Project {ProjectId} updated successfully by user {UserId}",
        projectId,
        userId);

    return project;
}

/// <summary>
/// Валидирует настройки FSRS
/// </summary>
private void ValidateFsrsSettings(JsonTypes.FsrsSettings settings)
{
    if (settings.RequestRetention < 0.7 || settings.RequestRetention > 0.99)
    {
        throw new ArgumentException("RequestRetention must be between 0.7 and 0.99");
    }

    if (settings.MaximumInterval < 1 || settings.MaximumInterval > 36500)
    {
        throw new ArgumentException("MaximumInterval must be between 1 and 36500");
    }

    if (settings.W != null && settings.W.Length != 18)
    {
        throw new ArgumentException("FSRS weights array must contain exactly 18 values");
    }
}
```

#### 1.2. IProjectService - добавить методы

**Файл:** `Polyraspad/VocabularyService/Services/IProjectService.cs`

```csharp
/// <summary>
/// Получает детали проекта по идентификатору
/// </summary>
Task<Project?> GetProjectByIdAsync(
    Guid projectId,
    Guid userId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Обновляет проект
/// </summary>
Task<Project> UpdateProjectAsync(
    Guid projectId,
    Guid userId,
    string? title = null,
    bool? isArchived = null,
    JsonTypes.FsrsSettings? fsrsSettings = null,
    CancellationToken cancellationToken = default);
```

#### 1.3. ContentService (gRPC) - реализовать методы

**Файл:** `Polyraspad/VocabularyService/Grpc/ContentService.cs`

**Добавить методы:**

```csharp
//===== SR-STR-02: Получение деталей проекта =====
/// <summary>
/// Получение полной информации о проекте, включая настройки FSRS (SR-STR-02)
/// </summary>
public override async Task<ProjectResponse> GetProjectDetails(
    GetProjectDetailsRequest request,
    ServerCallContext context)
{
    var userId = GrpcContextHelper.GetUserId(context);
    var roles = GrpcContextHelper.GetRoles(context);

    _logger.LogInformation(
        "GetProjectDetails request from user {UserId} for project {ProjectId}",
        userId,
        request.ProjectId);

    // Валидация UUID
    if (!Guid.TryParse(request.ProjectId, out var projectId))
    {
        throw new RpcException(
            new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
    }

    // Проверка user_id
    if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
    {
        if (requestUserId != userId)
        {
            throw new RpcException(
                new Status(StatusCode.PermissionDenied, "User ID mismatch"));
        }
    }

    // Получаем проект
    var project = await _projectService.GetProjectByIdAsync(projectId, userId, context.CancellationToken);
    
    if (project == null)
    {
        throw new RpcException(
            new Status(StatusCode.NotFound, $"Project {projectId} not found"));
    }

    // Преобразуем в ответ
    var response = _mapper.Map<ProjectResponse>(project);

    _logger.LogInformation(
        "Project {ProjectId} retrieved successfully for user {UserId}",
        projectId,
        userId);

    return response;
}

//===== SR-STR-02: Обновление настроек проекта =====
/// <summary>
/// Обновление метаданных и настроек алгоритма обучения (SR-STR-02)
/// </summary>
public override async Task<ProjectResponse> UpdateProject(
    UpdateProjectRequest request,
    ServerCallContext context)
{
    var userId = GrpcContextHelper.GetUserId(context);
    var roles = GrpcContextHelper.GetRoles(context);

    _logger.LogInformation(
        "UpdateProject request from user {UserId} for project {ProjectId}",
        userId,
        request.ProjectId);

    // Валидация UUID
    if (!Guid.TryParse(request.ProjectId, out var projectId))
    {
        throw new RpcException(
            new Status(StatusCode.InvalidArgument, "Invalid project ID format"));
    }

    // Проверка user_id
    if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
    {
        if (requestUserId != userId)
        {
            throw new RpcException(
                new Status(StatusCode.PermissionDenied, "User ID mismatch"));
        }
    }

    // Преобразуем gRPC запрос в параметры
    string? title = request.HasTitle ? request.Title.Value : null;
    bool? isArchived = request.HasIsArchived ? request.IsArchived.Value : null;
    JsonTypes.FsrsSettings? fsrsSettings = request.Settings != null
        ? _mapper.Map<JsonTypes.FsrsSettings>(request.Settings)
        : null;

    try
    {
        // Обновляем проект
        var project = await _projectService.UpdateProjectAsync(
            projectId,
            userId,
            title,
            isArchived,
            fsrsSettings,
            context.CancellationToken);

        // Преобразуем в ответ
        var response = _mapper.Map<ProjectResponse>(project);

        _logger.LogInformation(
            "Project {ProjectId} updated successfully by user {UserId}",
            projectId,
            userId);

        return response;
    }
    catch (KeyNotFoundException ex)
    {
        throw new RpcException(
            new Status(StatusCode.NotFound, ex.Message));
    }
    catch (ArgumentException ex)
    {
        throw new RpcException(
            new Status(StatusCode.InvalidArgument, ex.Message));
    }
    catch (UnauthorizedAccessException ex)
    {
        throw new RpcException(
            new Status(StatusCode.PermissionDenied, ex.Message));
    }
}
```

---

### 2. AggregatorService (REST API)

#### 2.1. VocabularyServiceClient - добавить методы

**Файл:** `Polyraspad/AggregatorService/Services/VocabularyServiceClient.cs`

**Добавить методы:**

```csharp
/// <summary>
/// Получает детали проекта из VocabularyService
/// </summary>
public async Task<ProjectResponse> GetProjectDetailsAsync(
    GetProjectDetailsRequest request,
    Guid userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default)
{
    try
    {
        var metadata = new Metadata
        {
            { "user_id", userId.ToString() },
            { "roles", string.Join(",", roles) }
        };

        request.UserId = userId.ToString();

        _logger.LogInformation(
            "Sending GetProjectDetails request to VocabularyService for user {UserId}, project {ProjectId}",
            userId,
            request.ProjectId);

        var response = await _client.GetProjectDetailsAsync(
            request,
            headers: metadata,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Project {ProjectId} retrieved successfully for user {UserId}",
            response.Id,
            userId);

        return response;
    }
    catch (RpcException ex)
    {
        _logger.LogError(ex, "gRPC error when getting project details for user {UserId}", userId);
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error when getting project details for user {UserId}", userId);
        throw;
    }
}

/// <summary>
/// Обновляет проект в VocabularyService
/// </summary>
public async Task<ProjectResponse> UpdateProjectAsync(
    UpdateProjectRequest request,
    Guid userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default)
{
    try
    {
        var metadata = new Metadata
        {
            { "user_id", userId.ToString() },
            { "roles", string.Join(",", roles) }
        };

        request.UserId = userId.ToString();

        _logger.LogInformation(
            "Sending UpdateProject request to VocabularyService for user {UserId}, project {ProjectId}",
            userId,
            request.ProjectId);

        var response = await _client.UpdateProjectAsync(
            request,
            headers: metadata,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Project {ProjectId} updated successfully for user {UserId}",
            response.Id,
            userId);

        return response;
    }
    catch (RpcException ex)
    {
        _logger.LogError(ex, "gRPC error when updating project for user {UserId}", userId);
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error when updating project for user {UserId}", userId);
        throw;
    }
}
```

#### 2.2. IVocabularyServiceClient - добавить методы

**Файл:** `Polyraspad/AggregatorService/Services/IVocabularyServiceClient.cs`

```csharp
/// <summary>
/// Получает детали проекта из VocabularyService
/// </summary>
Task<ProjectResponse> GetProjectDetailsAsync(
    GetProjectDetailsRequest request,
    Guid userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default);

/// <summary>
/// Обновляет проект в VocabularyService
/// </summary>
Task<ProjectResponse> UpdateProjectAsync(
    UpdateProjectRequest request,
    Guid userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default);
```

#### 2.3. ProjectsController - реализовать методы

**Файл:** `Polyraspad/AggregatorService/Controllers/ProjectsController.cs`

**Заменить заглушку GetProject:**

```csharp
//===== SR-STR-02: Получение деталей проекта =====
/// <summary>
/// Получает проект по идентификатору с настройками FSRS (SR-STR-02)
/// </summary>
/// <param name="id">Идентификатор проекта</param>
/// <returns>Детали проекта</returns>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<ProjectResponseDto>> GetProject(string id)
{
    try
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var roles = MappingHelper.GetRoles(User, Request.Headers);

        _logger.LogInformation(
            "GetProject request from user {UserId} for project {ProjectId}",
            userId,
            id);

        var grpcRequest = new GetProjectDetailsRequest
        {
            ProjectId = id
        };

        var grpcResponse = await _vocabularyServiceClient.GetProjectDetailsAsync(
            grpcRequest,
            userId,
            roles,
            HttpContext.RequestAborted);

        var responseDto = _mapper.Map<ProjectResponseDto>(grpcResponse);

        return Ok(responseDto);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Unauthorized access attempt");
        return Unauthorized(new { error = ex.Message });
    }
    catch (Grpc.Core.RpcException ex)
    {
        _logger.LogError(ex, "gRPC error when getting project");
        
        var statusCode = ex.StatusCode switch
        {
            Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
            Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new { error = ex.Status.Detail });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting project");
        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
    }
}
```

**Добавить метод UpdateProject:**

```csharp
//===== SR-STR-02: Обновление настроек проекта =====
/// <summary>
/// Обновляет метаданные и настройки проекта (SR-STR-02)
/// </summary>
/// <param name="id">Идентификатор проекта</param>
/// <param name="request">Данные для обновления</param>
/// <returns>Обновленный проект</returns>
[HttpPut("{id}")]
[ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<ProjectResponseDto>> UpdateProject(
    string id,
    [FromBody] UpdateProjectDto request)
{
    try
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var roles = MappingHelper.GetRoles(User, Request.Headers);

        _logger.LogInformation(
            "UpdateProject request from user {UserId} for project {ProjectId}",
            userId,
            id);

        var grpcRequest = _mapper.Map<UpdateProjectRequest>(request);
        grpcRequest.ProjectId = id;

        var grpcResponse = await _vocabularyServiceClient.UpdateProjectAsync(
            grpcRequest,
            userId,
            roles,
            HttpContext.RequestAborted);

        var responseDto = _mapper.Map<ProjectResponseDto>(grpcResponse);

        return Ok(responseDto);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Unauthorized access attempt");
        return Unauthorized(new { error = ex.Message });
    }
    catch (Grpc.Core.RpcException ex)
    {
        _logger.LogError(ex, "gRPC error when updating project");
        
        var statusCode = ex.StatusCode switch
        {
            Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
            Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new { error = ex.Status.Detail });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating project");
        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
    }
}
```

#### 2.4. DTOs - добавить UpdateProjectDto

**Файл:** `Polyraspad/AggregatorService/Dtos/UpdateProjectDto.cs` (создать новый)

```csharp
namespace AggregatorService.Dtos;

/// <summary>
/// DTO для обновления проекта
/// </summary>
public class UpdateProjectDto
{
    /// <summary>
    /// Название проекта (опционально)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Флаг архивации (опционально)
    /// </summary>
    public bool? IsArchived { get; set; }

    /// <summary>
    /// Настройки FSRS (опционально)
    /// </summary>
    public SrsSettingsDto? Settings { get; set; }
}

/// <summary>
/// DTO для настроек FSRS
/// </summary>
public class SrsSettingsDto
{
    /// <summary>
    /// Целевой процент удержания (0.7 - 0.99)
    /// </summary>
    public double RequestRetention { get; set; }

    /// <summary>
    /// Максимальный интервал в днях
    /// </summary>
    public int MaximumInterval { get; set; }

    /// <summary>
    /// Веса FSRS (18 значений)
    /// </summary>
    public double[]? W { get; set; }

    /// <summary>
    /// Включить краткосрочные интервалы
    /// </summary>
    public bool EnableShortTerm { get; set; }
}
```

#### 2.5. AutoMapper - добавить маппинги

**Файл:** `Polyraspad/AggregatorService/AutoMapperProfiles/AutoMappingProfile.cs`

```csharp
// UpdateProjectDto -> UpdateProjectRequest (gRPC)
CreateMap<UpdateProjectDto, UpdateProjectRequest>()
    .ForMember(dest => dest.Title, opt => opt.MapFrom(src => 
        src.Title != null ? new Google.Protobuf.WellKnownTypes.StringValue { Value = src.Title } : null))
    .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => 
        src.IsArchived.HasValue ? new Google.Protobuf.WellKnownTypes.BoolValue { Value = src.IsArchived.Value } : null))
    .ForMember(dest => dest.Settings, opt => opt.MapFrom((src, dest, destMember, context) => 
        src.Settings != null ? context.Mapper.Map<SrsSettings>(src.Settings) : null));

// SrsSettingsDto -> SrsSettings (gRPC)
CreateMap<SrsSettingsDto, SrsSettings>()
    .ForMember(dest => dest.W, opt => opt.MapFrom(src => src.W != null ? src.W.ToList() : new List<double>()));
```

---

## 🎨 Фронтенд (Frontend)

### 1. API Client - добавить методы

**Файл:** `Polyraspad/polyraspad-frontend/src/lib/api/client.ts`

**Добавить методы:**

```typescript
async getProject(id: string): Promise<ProjectResponseDto> {
  return this.request<ProjectResponseDto>(API_ENDPOINTS.PROJECTS.DETAIL(id))
}

async updateProject(id: string, data: UpdateProjectDto): Promise<ProjectResponseDto> {
  return this.request<ProjectResponseDto>(API_ENDPOINTS.PROJECTS.UPDATE(id), {
    method: "PUT",
    body: JSON.stringify(data),
  })
}
```

### 2. Types - добавить типы

**Файл:** `Polyraspad/polyraspad-frontend/src/lib/api/types.ts`

**Добавить:**

```typescript
export interface UpdateProjectDto {
  title?: string
  isArchived?: boolean
  settings?: SrsSettingsDto
}

// SrsSettingsDto уже должен быть определен, если нет - добавить:
export interface SrsSettingsDto {
  requestRetention: number
  maximumInterval: number
  w?: number[]
  enableShortTerm: boolean
}
```

### 3. Constants - добавить endpoints

**Файл:** `Polyraspad/polyraspad-frontend/src/lib/constants.ts`

```typescript
export const API_ENDPOINTS = {
  // ... existing endpoints
  PROJECTS: {
    LIST: "/api/projects",
    CREATE: "/api/projects",
    DETAIL: (id: string) => `/api/projects/${id}`,
    UPDATE: (id: string) => `/api/projects/${id}`,
  },
}
```

### 4. React Query - добавить queries/mutations

**Файл:** `Polyraspad/polyraspad-frontend/src/lib/react-query/queries.ts`

**Добавить:**

```typescript
export function useProject(id: string) {
  return useQuery({
    queryKey: queryKeys.project(id),
    queryFn: () => apiClient.getProject(id),
    enabled: !!id,
  })
}

export function useUpdateProject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProjectDto }) => 
      apiClient.updateProject(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.project(variables.id) })
      queryClient.invalidateQueries({ queryKey: queryKeys.projects })
    },
  })
}
```

**Добавить импорт:**

```typescript
import type { UpdateProjectDto } from "../api/types"
```

### 5. Страница деталей проекта

**Файл:** `Polyraspad/polyraspad-frontend/src/app/projects/[id]/page.tsx` (создать новый)

```typescript
"use client"

import { useParams } from "next/navigation"
import { useProject, useUpdateProject } from "@/lib/react-query/queries"
import { ProjectDetailsView } from "@/components/projects/project-details-view"

export default function ProjectDetailsPage() {
  const params = useParams()
  const projectId = params.id as string
  const { data: project, isLoading, error } = useProject(projectId)

  if (isLoading) {
    return <div>Loading...</div>
  }

  if (error || !project) {
    return <div>Project not found</div>
  }

  return <ProjectDetailsView project={project} />
}
```

### 6. Компонент деталей проекта

**Файл:** `Polyraspad/polyraspad-frontend/src/components/projects/project-details-view.tsx` (создать новый)

```typescript
"use client"

import { useState } from "react"
import { useUpdateProject } from "@/lib/react-query/queries"
import type { ProjectResponseDto, UpdateProjectDto } from "@/lib/api/types"
import { FsrsSettingsEditor } from "@/components/projects/fsrs-settings-editor"

interface ProjectDetailsViewProps {
  project: ProjectResponseDto
}

export function ProjectDetailsView({ project }: ProjectDetailsViewProps) {
  const [isEditing, setIsEditing] = useState(false)
  const [title, setTitle] = useState(project.title)
  const [settings, setSettings] = useState(project.settings)
  const updateProject = useUpdateProject()

  const handleSave = async () => {
    const updateData: UpdateProjectDto = {
      title,
      settings,
    }

    try {
      await updateProject.mutateAsync({ id: project.id, data: updateData })
      setIsEditing(false)
    } catch (error) {
      console.error("Failed to update project:", error)
    }
  }

  return (
    <div className="flex-1 overflow-y-auto p-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          {isEditing ? (
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="text-3xl font-bold bg-dark-800 border border-white/10 rounded-lg px-4 py-2 text-white"
            />
          ) : (
            <h1 className="text-3xl font-bold text-white">{project.title}</h1>
          )}
        </div>

        {/* Project Info */}
        <div className="glass-panel rounded-xl p-6 mb-6">
          <h2 className="text-xl font-bold text-white mb-4">Project Information</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm text-gray-400">Source Language</label>
              <p className="text-white">{project.sourceLang}</p>
            </div>
            <div>
              <label className="text-sm text-gray-400">Target Language</label>
              <p className="text-white">{project.targetLang}</p>
            </div>
            <div>
              <label className="text-sm text-gray-400">Total Lemmas</label>
              <p className="text-white">{project.stats?.totalLemmas || 0}</p>
            </div>
            <div>
              <label className="text-sm text-gray-400">Mature Lemmas</label>
              <p className="text-white">{project.stats?.matureLemmas || 0}</p>
            </div>
          </div>
        </div>

        {/* FSRS Settings */}
        <div className="glass-panel rounded-xl p-6 mb-6">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-xl font-bold text-white">FSRS Settings</h2>
            {!isEditing && (
              <button
                onClick={() => setIsEditing(true)}
                className="px-4 py-2 bg-brand-purple hover:bg-indigo-600 text-white rounded-lg transition-colors"
              >
                Edit
              </button>
            )}
          </div>

          {isEditing ? (
            <FsrsSettingsEditor
              settings={settings}
              onChange={setSettings}
              onSave={handleSave}
              onCancel={() => {
                setIsEditing(false)
                setTitle(project.title)
                setSettings(project.settings)
              }}
              isLoading={updateProject.isPending}
            />
          ) : (
            <div className="space-y-4">
              <div>
                <label className="text-sm text-gray-400">Request Retention</label>
                <p className="text-white">{(settings?.requestRetention || 0) * 100}%</p>
              </div>
              <div>
                <label className="text-sm text-gray-400">Maximum Interval</label>
                <p className="text-white">{settings?.maximumInterval || 0} days</p>
              </div>
              <div>
                <label className="text-sm text-gray-400">Enable Short Term</label>
                <p className="text-white">{settings?.enableShortTerm ? "Yes" : "No"}</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
```

### 7. Компонент редактора FSRS настроек

**Файл:** `Polyraspad/polyraspad-frontend/src/components/projects/fsrs-settings-editor.tsx` (создать новый)

```typescript
"use client"

import type { SrsSettingsDto } from "@/lib/api/types"

interface FsrsSettingsEditorProps {
  settings?: SrsSettingsDto
  onChange: (settings: SrsSettingsDto) => void
  onSave: () => void
  onCancel: () => void
  isLoading: boolean
}

export function FsrsSettingsEditor({
  settings,
  onChange,
  onSave,
  onCancel,
  isLoading,
}: FsrsSettingsEditorProps) {
  const requestRetention = settings?.requestRetention ?? 0.9
  const maximumInterval = settings?.maximumInterval ?? 36500
  const enableShortTerm = settings?.enableShortTerm ?? true

  const handleRequestRetentionChange = (value: number) => {
    onChange({
      ...settings,
      requestRetention: value,
      maximumInterval: settings?.maximumInterval ?? 36500,
      enableShortTerm: settings?.enableShortTerm ?? true,
      w: settings?.w,
    })
  }

  const handleMaximumIntervalChange = (value: number) => {
    onChange({
      ...settings,
      requestRetention: settings?.requestRetention ?? 0.9,
      maximumInterval: value,
      enableShortTerm: settings?.enableShortTerm ?? true,
      w: settings?.w,
    })
  }

  return (
    <div className="space-y-6">
      {/* Request Retention */}
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Request Retention: {(requestRetention * 100).toFixed(1)}%
        </label>
        <input
          type="range"
          min="0.7"
          max="0.99"
          step="0.01"
          value={requestRetention}
          onChange={(e) => handleRequestRetentionChange(parseFloat(e.target.value))}
          className="w-full"
        />
        <p className="text-xs text-gray-500 mt-1">
          Target percentage of cards remembered (70% - 99%)
        </p>
      </div>

      {/* Maximum Interval */}
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Maximum Interval: {maximumInterval} days
        </label>
        <input
          type="number"
          min="1"
          max="36500"
          value={maximumInterval}
          onChange={(e) => handleMaximumIntervalChange(parseInt(e.target.value))}
          className="w-full px-3 py-2 border border-white/10 rounded-lg bg-dark-800 text-white"
        />
        <p className="text-xs text-gray-500 mt-1">
          Maximum days between reviews (1 - 36500)
        </p>
      </div>

      {/* Enable Short Term */}
      <div>
        <label className="flex items-center gap-2">
          <input
            type="checkbox"
            checked={enableShortTerm}
            onChange={(e) =>
              onChange({
                ...settings,
                requestRetention: settings?.requestRetention ?? 0.9,
                maximumInterval: settings?.maximumInterval ?? 36500,
                enableShortTerm: e.target.checked,
                w: settings?.w,
              })
            }
            className="rounded"
          />
          <span className="text-sm text-gray-300">Enable Short Term Intervals</span>
        </label>
      </div>

      {/* Actions */}
      <div className="flex gap-3 justify-end pt-4 border-t border-white/10">
        <button
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-2 text-gray-300 bg-dark-700 hover:bg-dark-600 rounded-lg transition-colors disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          onClick={onSave}
          disabled={isLoading}
          className="px-4 py-2 bg-brand-purple hover:bg-indigo-600 text-white rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {isLoading ? "Saving..." : "Save"}
        </button>
      </div>
    </div>
  )
}
```

### 8. Обновить навигацию

**Файл:** `Polyraspad/polyraspad-frontend/src/components/projects/project-card.tsx`

**Добавить ссылку на детали:**

```typescript
// В компоненте ProjectCard добавить onClick или Link
<Link href={`/projects/${project.id}`}>
  {/* ... existing card content ... */}
</Link>
```

---

## ✅ Чеклист реализации

### Backend
- [ ] Добавить `GetProjectByIdAsync` в `ProjectService`
- [ ] Добавить `UpdateProjectAsync` в `ProjectService`
- [ ] Обновить `IProjectService` интерфейс
- [ ] Реализовать `GetProjectDetails` в `ContentService` (gRPC)
- [ ] Реализовать `UpdateProject` в `ContentService` (gRPC)
- [ ] Добавить методы в `VocabularyServiceClient` (AggregatorService)
- [ ] Обновить `IVocabularyServiceClient` интерфейс
- [ ] Реализовать `GetProject` в `ProjectsController`
- [ ] Реализовать `UpdateProject` в `ProjectsController`
- [ ] Создать `UpdateProjectDto`
- [ ] Добавить AutoMapper маппинги

### Frontend
- [ ] Добавить методы в `apiClient`
- [ ] Добавить типы `UpdateProjectDto` и `SrsSettingsDto`
- [ ] Добавить endpoints в constants
- [ ] Добавить React Query hooks
- [ ] Создать страницу `/projects/[id]/page.tsx`
- [ ] Создать компонент `ProjectDetailsView`
- [ ] Создать компонент `FsrsSettingsEditor`
- [ ] Обновить `ProjectCard` для навигации

---

## 📝 Примечания

1. **Валидация FSRS**: На бэкенде нужно валидировать диапазоны значений (requestRetention: 0.7-0.99, maximumInterval: 1-36500)

2. **Веса FSRS**: Массив `w` должен содержать 18 значений. Если не передан, использовать дефолтные веса.

3. **Частичное обновление**: Использовать `google.protobuf.StringValue` и `google.protobuf.BoolValue` для опциональных полей в gRPC.

4. **Обработка ошибок**: На фронтенде нужно показывать понятные сообщения об ошибках валидации.

5. **Оптимистичные обновления**: Можно добавить optimistic updates в React Query для лучшего UX.

