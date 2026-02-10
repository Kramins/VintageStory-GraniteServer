using System;

namespace Granite.Common.Dto;

public record StoredChunkHashDTO(int ChunkX, int ChunkZ, string ContentHash);
