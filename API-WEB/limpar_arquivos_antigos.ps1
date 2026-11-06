# Move para fora do projeto os arquivos da versao antiga que nao existem mais.
# Como usar: clique com o botao direito neste arquivo > "Executar com o PowerShell"
# (ou abra o PowerShell na pasta do projeto e rode: .\limpar_arquivos_antigos.ps1)
# Nada e apagado: os arquivos vao para a pasta _SistemaWhiteLabel_arquivos_antigos,
# ao lado da pasta do projeto.

$destino = Join-Path (Split-Path $PSScriptRoot -Parent) "_SistemaWhiteLabel_arquivos_antigos"

$arquivos = @(
    "Controllers\FiadoController.cs",
    "DTOs\FiadoDto.cs",
    "DTOs\HistoricoFiadoDto.cs",
    "DTOs\PagamentoFiadoDto.cs",
    "Models\Cliente.cs",
    "Repositories\ClienteRepository.cs",
    "Repositories\Interfaces\IClienteRepository.cs",
    "Migrations",                  # migracoes do EF: o banco agora muda pelos scripts da pasta Banco
    "docker-compose.yml",         # nao e mais usado
    "wwwroot\novaVenda.html",      # telas do front antigo (substituidas pelo novo front)
    "wwwroot\fiado.html"
)

$movidos = 0
foreach ($arquivo in $arquivos) {
    $origem = Join-Path $PSScriptRoot $arquivo
    if (Test-Path $origem) {
        $novo = Join-Path $destino $arquivo
        New-Item -ItemType Directory -Force -Path (Split-Path $novo -Parent) | Out-Null
        Move-Item -Path $origem -Destination $novo -Force
        Write-Host "Movido: $arquivo"
        $movidos++
    }
}

if ($movidos -eq 0) {
    Write-Host "Nenhum arquivo antigo encontrado. Sua pasta ja esta limpa."
} else {
    Write-Host ""
    Write-Host "$movidos arquivo(s) movido(s) para: $destino"
}
Write-Host ""
Write-Host "Agora, no Visual Studio: Compilacao > Limpar Solucao, depois Recompilar Solucao."
