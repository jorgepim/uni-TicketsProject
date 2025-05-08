
CREATE DATABASE TicketDb;

USE TicketDb;

-- Tabla Usuarios
CREATE TABLE Usuarios (
    UsuarioId INT PRIMARY KEY IDENTITY,
    Nombre NVARCHAR(100),
    Apellido NVARCHAR(100),
    Email NVARCHAR(150) UNIQUE NOT NULL,
    Telefono NVARCHAR(20),
    TipoUsuario VARCHAR(20) CHECK (TipoUsuario IN ('Interno', 'Externo')),
    MetodoAutenticacion VARCHAR(20) CHECK (MetodoAutenticacion IN ('Correo')),
    ContrasenaHash NVARCHAR(MAX),
    FechaRegistro DATETIME DEFAULT GETDATE(),
    Estado BIT CHECK (Estado IN (0, 1))
);

-- Tabla EmpresasExternas
CREATE TABLE EmpresasExternas (
    EmpresaId INT PRIMARY KEY IDENTITY,
    NombreEmpresa NVARCHAR(100),
    ContactoPrincipal NVARCHAR(100),
    TelefonoEmpresa NVARCHAR(20),
    DireccionEmpresa NVARCHAR(200)
);

-- Tabla ClientesExternos
CREATE TABLE ClientesExternos (
    UsuarioId INT PRIMARY KEY,
    EmpresaId INT,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (EmpresaId) REFERENCES EmpresasExternas(EmpresaId)
);

-- Tabla Roles
CREATE TABLE Roles (
    RolId INT PRIMARY KEY,
    NombreRol VARCHAR(50) UNIQUE
);

-- Tabla Usuarios_Roles
CREATE TABLE Usuarios_Roles (
    UsuarioId INT,
    RolId INT,
    PRIMARY KEY (UsuarioId, RolId),
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (RolId) REFERENCES Roles(RolId)
);

-- Tabla Categorias
CREATE TABLE Categorias (
    CategoriaId INT PRIMARY KEY IDENTITY,
    Nombre NVARCHAR(100),
    Descripcion NVARCHAR(255)
);

-- Tabla Usuarios_Categorias
CREATE TABLE Usuarios_Categorias (
    UsuarioId INT,
    CategoriaId INT,
    PRIMARY KEY (UsuarioId, CategoriaId),
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (CategoriaId) REFERENCES Categorias(CategoriaId)
);

-- Tabla EstadosTicket
CREATE TABLE EstadosTicket (
    EstadoId INT PRIMARY KEY,
    NombreEstado NVARCHAR(50) UNIQUE
);

-- Tabla Tickets
CREATE TABLE Tickets (
    TicketId INT PRIMARY KEY IDENTITY,
    UsuarioCreadorId INT,
    CategoriaId INT,
    Titulo NVARCHAR(100),
    AplicacionAfectada NVARCHAR(100),
    Descripcion NVARCHAR(MAX),
    Prioridad VARCHAR(20) CHECK (Prioridad IN ('Alta', 'Media', 'Baja')),
    EstadoId INT,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    FechaCierre DATETIME,
    FOREIGN KEY (UsuarioCreadorId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (CategoriaId) REFERENCES Categorias(CategoriaId),
    FOREIGN KEY (EstadoId) REFERENCES EstadosTicket(EstadoId)
);

-- Tabla HistorialEstadosTicket
CREATE TABLE HistorialEstadosTicket (
    HistorialId INT PRIMARY KEY IDENTITY,
    TicketId INT,
    EstadoAnterior INT,
    EstadoNuevo INT,
    UsuarioCambioId INT,
    FechaCambio DATETIME DEFAULT GETDATE(),
    Comentarios NVARCHAR(500),
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId),
    FOREIGN KEY (EstadoAnterior) REFERENCES EstadosTicket(EstadoId),
    FOREIGN KEY (EstadoNuevo) REFERENCES EstadosTicket(EstadoId),
    FOREIGN KEY (UsuarioCambioId) REFERENCES Usuarios(UsuarioId)
);

-- Tabla Adjuntos
CREATE TABLE Adjuntos (
    AdjuntoId INT PRIMARY KEY IDENTITY,
    TicketId INT,
    NombreArchivo NVARCHAR(255),
    RutaArchivo NVARCHAR(500),
    FechaSubida DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId)
);

-- Tabla Asignaciones
CREATE TABLE Asignaciones (
    AsignacionId INT PRIMARY KEY IDENTITY,
    TicketId INT,
    UsuarioAsignadoId INT,
    FechaAsignacion DATETIME DEFAULT GETDATE(),
    ComentarioAsignacion NVARCHAR(255),
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId),
    FOREIGN KEY (UsuarioAsignadoId) REFERENCES Usuarios(UsuarioId)
);

-- Tabla TareasColaborativas
CREATE TABLE TareasColaborativas (
    TareaId INT PRIMARY KEY IDENTITY,
    TicketId INT,
    AsignadorId INT,
    UsuarioDestinoId INT,
    Descripcion NVARCHAR(500),
    FechaAsignacion DATETIME DEFAULT GETDATE(),
    Estado VARCHAR(20) CHECK (Estado IN ('Pendiente', 'EnProgreso', 'Completada','Cancelado','Rechazado','EsperaInformacion')),
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId),
    FOREIGN KEY (AsignadorId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (UsuarioDestinoId) REFERENCES Usuarios(UsuarioId)
);

-- Tabla ComentariosTicket
CREATE TABLE ComentariosTicket (
    ComentarioId INT PRIMARY KEY IDENTITY,
    TicketId INT,
    UsuarioId INT,
    Comentario NVARCHAR(MAX),
    FechaComentario DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId),
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId)
);

-- Tabla Notificaciones
CREATE TABLE Notificaciones (
    NotificacionId INT PRIMARY KEY IDENTITY,
    UsuarioId INT,
    TicketId INT,
    Mensaje NVARCHAR(255),
    FechaEnvio DATETIME DEFAULT GETDATE(),
    Leido BIT DEFAULT 0 CHECK (Leido IN (0, 1)),
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId)
);
