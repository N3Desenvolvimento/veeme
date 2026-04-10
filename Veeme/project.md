# Especificações do Projeto (Veeme - Estoque)

## Objetivo

- Aplicativo mobile-first para gerenciamento de estoque, com foco em telas pequenas e uso otimizado do espaço vertical.

## Princípios de UI/UX (Mobile-first)

- Priorizar dispositivos móveis e telas pequenas (layout deve “caber” sem desperdício de altura).
- Evitar cabeçalhos grandes; preferir cabeçalho compacto (altura reduzida) quando necessário.
- Preferir botões grandes o suficiente para toque, mas com espaçamento eficiente (sem excesso de padding vertical).
- Navegação principal deve ser simples e direta, com poucos níveis.

## Navegação (Tela Inicial)

- Rota: `/`
- Deve funcionar como “hub” do app (tela de navegação principal).
- Deve ter 4 botões iniciais:
  1. Localizar produtos (habilitado)
  2. Opção 2 (desabilitado)
  3. Opção 3 (desabilitado)
  4. Opção 4 (desabilitado)
- Os botões 2, 3 e 4 devem estar desabilitados e não devem indicar a funcionalidade final (apenas placeholders).
- Os botões devem ter altura um pouco maior para facilitar toque em telas pequenas.

## Tela: Localizar Produtos

- Rota: `/produtos/localizar`
- Ao entrar nesta tela, exibir um cabeçalho bem compacto com:
  - Botão de voltar para a tela inicial
  - Título curto
- Conteúdo principal:
  - Campos de busca por:
    - Nome/descrição do produto
    - Código de barras
  - Botão “Buscar”
  - Lista de resultados
- Ao bipar um produto (coletor), o campo de código de barras deve ser preenchido automaticamente e a busca deve ser executada automaticamente.
- A lista deve ser otimizada para leitura em tela pequena (cartões compactos ou linhas bem enxutas).

## API: Produtos

- Endpoint: `GET /api/produtos`
- Query string:
  - `nome` (opcional) para buscar por descrição
  - `codigoBarras` (opcional) para buscar por código de barras
- Resposta deve retornar apenas os 100 primeiros registros (limite fixo).
- A busca deve usar colunas fixas (sem tentativa de inferir nomes de colunas):
  - Nome do produto: `DESCRICAO`
  - Código de barras: `CODIGO_BARRA`

## Banco de Dados

- Banco Firebird acessado via Dapper.
- Conexão deve ser configurável via `appsettings.json`.
- Charset deve ser compatível com Firebird (ex.: `ISO8859_1`), evitando valores inválidos como `WIN1252`.

## Regras de Implementação

- Não implementar “candidates”/descoberta automática de colunas para a tabela `PRODUTOS`; usar sempre colunas conhecidas.
- Consultas devem ser parametrizadas (evitar concatenação insegura em SQL).
- Sempre validar o build após mudanças (corrigir erros de compilação e runtime que impeçam o app de rodar).

## Diretrizes de Design (UI/UX)

- Gostaria que você trabalhase visualmente no app para deixa-lo com uma aprencia bonita e inovadora com aspectos futuristas que indique que nossa aplicação é bem moderna para chamar a atneção do usuário com as melhroes praticas de ui e ux deisng, além de utilização de icones para identificação de pontos importante da aplicação e cores viva e chamativas, e com um padrão de taanhospara bot~eos larguras, paddings, margis, space, etc.
- As telas devem manter consistência visual (mesmos tamanhos de botão, larguras, paddings, margens e espaçamentos) em todo o app.

## Debug de API

- O projeto deve disponibilizar Swagger em ambiente de desenvolvimento para facilitar debug.
