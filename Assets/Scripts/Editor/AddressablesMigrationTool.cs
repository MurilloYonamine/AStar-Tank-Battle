// Ferramenta de Editor do UmaTank: migra as referências diretas (ModelPrefab/VoiceClip) para Addressables.
using System.Collections.Generic; // Fornece List, Dictionary e HashSet, usados para acumular dados durante a migração.
using System.Text; // Fornece StringBuilder, que monta o resumo enviado ao Console.
using UnityEditor; // Fornece MenuItem, Undo, SerializedObject, AssetDatabase, EditorUtility e EditorApplication.
using UnityEditor.AddressableAssets; // Fornece AddressableAssetSettingsDefaultObject, o ponto de acesso às configurações do Addressables.
using UnityEditor.AddressableAssets.Settings; // Fornece AddressableAssetSettings, AddressableAssetGroup e AddressableAssetEntry.
using UnityEditor.AddressableAssets.Settings.GroupSchemas; // Fornece BundledAssetGroupSchema e ContentUpdateGroupSchema (regras de cada grupo).
using UnityEditor.SceneManagement; // Fornece EditorSceneManager, usado para marcar a cena como modificada.
using UnityEngine; // Fornece Debug, Component e UnityEngine.Object.
using UnityEngine.SceneManagement; // Fornece SceneManager e Scene, usados para percorrer as cenas abertas.

public static class AddressablesMigrationTool { // Classe estática: só tem itens de menu e funções auxiliares (não precisa de instância).
    private const string MenuMigrar = "UmaTank/Addressables/1 - Migrar personagens"; // Caminho do item de menu da etapa 1.
    private const string MenuLimpar = "UmaTank/Addressables/2 - Limpar referências diretas"; // Caminho do item de menu da etapa 2.
    private const string GrupoModelos = "Characters_Models"; // Nome do grupo Addressable que guarda os prefabs dos personagens.
    private const string GrupoVozes = "Characters_Voices"; // Nome do grupo Addressable que guarda os clipes de voz.
    private const string UndoMigrar = "UmaTank: migrar personagens para Addressables"; // Nome do passo de Undo da etapa 1 (aparece em Edit > Undo).
    private const string UndoLimpar = "UmaTank: limpar referências diretas"; // Nome do passo de Undo da etapa 2.
    private const string Prefixo = "[AddressablesMigrationTool] "; // Prefixo das mensagens, para filtrar no Console.
    private const string CampoGuid = "m_AssetGUID"; // Nome do campo serializado do AssetReference que guarda o GUID do asset.

    private sealed class Contagem { // Guarda os números do resumo de uma categoria (uma instância para modelos, outra para vozes).
        public int Novos; // Assets que não eram Addressable e foram marcados agora.
        public int Movidos; // Assets que já eram Addressable em OUTRO grupo e foram movidos para o grupo certo.
        public int JaEstavam; // Assets que já estavam Addressable no grupo certo.
        public int EnderecosAjustados; // Entre os que já estavam, quantos tiveram o address corrigido.
        public int Compartilhados; // Itens que usam um asset já tratado em outro item (ex.: a mesma voz em duas skins).
        public int Ignorados; // Itens cujo asset não pôde virar Addressable (fora de Assets/, por exemplo).
        public int SemReferenciaDireta; // Itens sem ModelPrefab/VoiceClip (já limpos, ou voz opcional vazia).
        public int RefsPreenchidas; // Campos ModelRef/VoiceRef escritos ou corrigidos agora.
        public int RefsJaCorretas; // Campos ModelRef/VoiceRef que já tinham o GUID certo.
    } // fim da classe Contagem

    private sealed class Plano { // Guarda o resultado da verificação feita antes de limpar.
        public readonly List<string> Problemas = new List<string>(); // Motivos que impedem a limpeza (lista vazia = pode limpar).
        public int Modelos; // Quantos ModelPrefab ainda estão preenchidos e serão zerados.
        public int Vozes; // Quantos VoiceClip ainda estão preenchidos e serão zerados.
    } // fim da classe Plano

    // ------------------------------------------------------------------ ETAPA 1: MIGRAR

    [MenuItem(MenuMigrar, false, 1)] // Cria o item de menu da etapa 1 (prioridade 1 = fica acima da etapa 2).
    public static void Migrar() { // Marca prefabs/clipes como Addressable e preenche ModelRef/VoiceRef.
        if (!PodeRodar(out List<SerializedObject> objetos)) return; // Aborta se estiver em Play Mode ou se não houver controller na cena.
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true); // Obtém as configurações do Addressables (cria se não existirem).
        if (settings == null) { // Se mesmo assim não houver configurações...
            Erro("Não foi possível obter nem criar as configurações do Addressables."); // ...avisa no Console e em uma janela.
            return; // ...e encerra sem mexer em nada.
        } // fim do if (settings == null)

        StringBuilder log = new StringBuilder(); // Acumula os avisos que entram no resumo final.
        Undo.IncrementCurrentGroup(); // Abre um novo grupo de Undo, separado de ações anteriores.
        Undo.SetCurrentGroupName(UndoMigrar); // Dá nome ao grupo de Undo.
        int grupoUndo = Undo.GetCurrentGroup(); // Guarda o índice para juntar tudo em um único Ctrl+Z no final.

        AddressableAssetGroup grupoModelos = ObterOuCriarGrupo(settings, GrupoModelos, log); // Garante que o grupo de modelos existe.
        AddressableAssetGroup grupoVozes = ObterOuCriarGrupo(settings, GrupoVozes, log); // Garante que o grupo de vozes existe.
        Contagem modelos = new Contagem(); // Contadores dos modelos.
        Contagem vozes = new Contagem(); // Contadores das vozes.

        foreach (SerializedObject so in objetos) { // Para cada controller encontrado (normalmente só um)...
            MigrarController(so, settings, grupoModelos, grupoVozes, modelos, vozes, log); // ...migra todos os seus personagens.
        } // fim do foreach

        AssetDatabase.SaveAssets(); // Grava em disco os assets do Addressables alterados (grupos e configurações).
        Undo.CollapseUndoOperations(grupoUndo); // Junta todas as operações acima em um único passo de Undo.

        StringBuilder resumo = new StringBuilder(); // Monta a mensagem final do Console.
        resumo.AppendLine(Prefixo + "Migração concluída."); // Título do resumo.
        AnexarResumo(resumo, "Modelos", grupoModelos, modelos); // Linhas com os números dos modelos.
        AnexarResumo(resumo, "Vozes  ", grupoVozes, vozes); // Linhas com os números das vozes.
        resumo.Append(log); // Anexa os avisos e informações acumulados durante o processo.
        resumo.AppendLine("Próximos passos: salve a cena (Ctrl+S), confira em Window > Asset Management > Addressables > Groups e só então rode o item 2."); // Lembrete de uso.
        Debug.Log(resumo.ToString()); // Imprime o resumo no Console.
    } // fim de Migrar

    private static void MigrarController(SerializedObject so, AddressableAssetSettings settings, AddressableAssetGroup grupoModelos, AddressableAssetGroup grupoVozes, Contagem modelos, Contagem vozes, StringBuilder log) { // Migra todos os personagens de um controller.
        so.Update(); // Sincroniza o SerializedObject com o estado atual do componente.
        SerializedProperty lista = so.FindProperty("characters"); // Localiza o array privado "characters" (campo [SerializeField]).
        Dictionary<string, string> modelosTratados = new Dictionary<string, string>(); // GUID -> address já definido nesta execução (detecta modelo repetido).
        Dictionary<string, string> vozesTratadas = new Dictionary<string, string>(); // Igual ao anterior, mas para vozes (há clipes compartilhados entre skins).
        HashSet<string> enderecosUsados = new HashSet<string>(); // Todos os addresses já usados nesta execução (detecta colisão de nomes).

        for (int i = 0; i < lista.arraySize; i++) { // Percorre os personagens na ordem do array (ordem fixa = resultado determinístico).
            SerializedProperty item = lista.GetArrayElementAtIndex(i); // Pega o item i (um CharacterOption).
            string nome = NomeDoItem(item, i); // Nome legível para as mensagens do Console.
            string baseEndereco = $"char/{IdSeguro(item, i)}/{AparenciaSegura(item)}/"; // Prefixo do address: char/<CharacterId>/<AppearanceName>/.
            Processar(item.FindPropertyRelative("ModelPrefab"), item.FindPropertyRelative("ModelRef"), baseEndereco + "model", nome, settings, grupoModelos, modelosTratados, enderecosUsados, modelos, log); // Trata o modelo 3D.
            Processar(item.FindPropertyRelative("VoiceClip"), item.FindPropertyRelative("VoiceRef"), baseEndereco + "voice", nome, settings, grupoVozes, vozesTratadas, enderecosUsados, vozes, log); // Trata a voz.
        } // fim do for

        if (so.ApplyModifiedProperties()) { // Grava as mudanças no componente (registra Undo); retorna true só se algo mudou.
            EditorSceneManager.MarkSceneDirty(((Component)so.targetObject).gameObject.scene); // Marca a cena como modificada para o Unity pedir para salvar.
        } // fim do if
    } // fim de MigrarController

    private static void Processar(SerializedProperty direto, SerializedProperty referencia, string endereco, string nome, AddressableAssetSettings settings, AddressableAssetGroup grupo, Dictionary<string, string> tratados, HashSet<string> enderecosUsados, Contagem c, StringBuilder log) { // Trata um par (referência direta, referência Addressable).
        Object asset = direto.objectReferenceValue; // Asset ligado diretamente (prefab ou clipe); pode ser nulo.
        if (asset == null) { // Sem referência direta: já foi limpo antes, ou é uma voz opcional vazia.
            c.SemReferenciaDireta++; // Conta para o resumo.
            return; // Não há nada para marcar; o ModelRef/VoiceRef existente fica como está (isso torna a rotina idempotente).
        } // fim do if (asset == null)

        string caminho = AssetDatabase.GetAssetPath(asset); // Caminho do asset no projeto (ex.: Assets/Models/X.prefab).
        if (string.IsNullOrEmpty(caminho) || !caminho.StartsWith("Assets/")) { // Objeto de cena, asset embutido ou de pacote não pode virar Addressable.
            c.Ignorados++; // Conta para o resumo.
            log.AppendLine($"AVISO: {nome}: '{asset.name}' não está em Assets/ (caminho '{caminho}'); foi ignorado."); // Explica o motivo no Console.
            return; // Segue para o próximo sem criar entrada nem preencher a referência.
        } // fim do if (caminho inválido)

        string guid = AssetDatabase.AssetPathToGUID(caminho); // GUID do asset: é o que o AssetReference guarda.
        bool entradaOk = true; // Indica se o asset está Addressable ao final (necessário para preencher a referência).
        if (tratados.TryGetValue(guid, out string enderecoJaDefinido)) { // Este mesmo asset já foi tratado por um item anterior desta execução.
            c.Compartilhados++; // Conta para o resumo.
            log.AppendLine($"INFO: {nome}: usa o mesmo asset de outro item; mantém o address '{enderecoJaDefinido}' (um asset só pode ter um address)."); // Documenta o compartilhamento.
        } else { // Primeira vez que este asset aparece nesta execução.
            string enderecoFinal = endereco; // Começa com o address desejado.
            int sufixo = 2; // Contador usado só se houver colisão de nomes.
            while (!enderecosUsados.Add(enderecoFinal)) { // Add retorna false se o address já foi usado por outro asset...
                enderecoFinal = endereco + "_" + sufixo++; // ...então acrescenta _2, _3... até ficar único.
            } // fim do while
            if (enderecoFinal != endereco) { // Se precisou mudar o nome...
                log.AppendLine($"AVISO: {nome}: address '{endereco}' já existia; usado '{enderecoFinal}'. Confira CharacterId/AppearanceName duplicados."); // ...avisa o usuário.
            } // fim do if
            tratados[guid] = enderecoFinal; // Lembra o address deste asset para detectar repetições.
            entradaOk = GarantirEntrada(settings, grupo, guid, enderecoFinal, nome, c, log); // Marca como Addressable no grupo certo, com o address certo.
        } // fim do if/else

        if (!entradaOk) return; // Se não deu para marcar o asset, não preenche a referência (evita um ModelRef apontando para algo não Addressable).
        if (EscreverGuid(referencia, guid)) c.RefsPreenchidas++; // Escreve o GUID em ModelRef/VoiceRef; conta se mudou algo...
        else c.RefsJaCorretas++; // ...ou conta como já correto (segunda execução cai aqui).
    } // fim de Processar

    private static bool GarantirEntrada(AddressableAssetSettings settings, AddressableAssetGroup grupo, string guid, string endereco, string nome, Contagem c, StringBuilder log) { // Deixa o asset Addressable no grupo e com o address indicados.
        AddressableAssetEntry entrada = settings.FindAssetEntry(guid); // Procura uma entrada já existente, em qualquer grupo.
        bool jaNoGrupo = entrada != null && entrada.parentGroup == grupo; // True se já está no grupo de destino.
        bool emOutroGrupo = entrada != null && !jaNoGrupo; // True se já é Addressable, mas em outro grupo (ex.: Default Local Group).

        Undo.RecordObject(grupo, UndoMigrar); // Registra o estado do grupo de destino ANTES de alterá-lo (permite desfazer).
        if (emOutroGrupo && entrada.parentGroup != null) { // Se vai tirar a entrada de outro grupo...
            Undo.RecordObject(entrada.parentGroup, UndoMigrar); // ...registra também o grupo de origem.
        } // fim do if

        if (!jaNoGrupo) { // Só cria/move quando necessário (idempotência: não mexe no que já está certo).
            entrada = settings.CreateOrMoveEntry(guid, grupo, false, true); // Cria a entrada, ou move a existente para o grupo de destino.
        } // fim do if

        if (entrada == null) { // Se o Addressables recusou o asset (ex.: está numa pasta Resources)...
            c.Ignorados++; // ...conta para o resumo...
            log.AppendLine($"AVISO: {nome}: o Addressables recusou o asset {guid}; ele foi ignorado."); // ...e avisa.
            return false; // Informa ao chamador que não há entrada válida.
        } // fim do if (entrada == null)

        if (entrada.address != endereco) { // Se o address atual é diferente do desejado (um asset novo vem com o caminho como address)...
            entrada.SetAddress(endereco); // ...define o address no padrão char/<CharacterId>/<AppearanceName>/model|voice.
            if (jaNoGrupo) c.EnderecosAjustados++; // Conta como "ajustado" apenas quando o asset já estava no grupo certo.
        } // fim do if

        if (jaNoGrupo) c.JaEstavam++; // Já estava no grupo de destino.
        else if (emOutroGrupo) c.Movidos++; // Foi movido de outro grupo.
        else c.Novos++; // Foi marcado como Addressable agora.
        return true; // Sucesso: o asset está Addressable no grupo certo.
    } // fim de GarantirEntrada

    private static AddressableAssetGroup ObterOuCriarGrupo(AddressableAssetSettings settings, string nome, StringBuilder log) { // Devolve o grupo pelo nome, criando-o se não existir.
        AddressableAssetGroup grupo = settings.FindGroup(nome); // Procura um grupo com esse nome.
        if (grupo != null) { // Se já existe...
            BundledAssetGroupSchema existente = grupo.GetSchema<BundledAssetGroupSchema>(); // ...lê a configuração de empacotamento dele.
            if (existente == null) { // Grupo sem regra de empacotamento: não gera bundles.
                log.AppendLine($"AVISO: o grupo '{nome}' já existe mas não tem o schema 'Content Packing & Loading'; configure-o no Inspector."); // Avisa; não altera grupos existentes.
            } else if (existente.BundleMode != BundledAssetGroupSchema.BundlePackingMode.PackSeparately) { // Grupo existente com outro modo de empacotamento.
                log.AppendLine($"AVISO: o grupo '{nome}' já existe com Bundle Mode '{existente.BundleMode}' (esperado: PackSeparately); mantido como está."); // Avisa sem sobrescrever (você pode ter mudado de propósito).
            } // fim do if/else
            return grupo; // Reaproveita o grupo existente sem alterar sua configuração.
        } // fim do if (grupo != null)

        grupo = settings.CreateGroup(nome, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema)); // Cria o grupo (não é o padrão, editável) com os dois schemas usuais.
        BundledAssetGroupSchema schema = grupo.GetSchema<BundledAssetGroupSchema>(); // Pega o schema recém-criado para configurá-lo.
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately; // Um bundle pequeno por asset (carrega/descarrega só o necessário).
        bool buildOk = schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath); // Caminho de BUILD = Local (variável do Profile).
        bool loadOk = schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath); // Caminho de CARGA = Local (variável do Profile).
        if (!buildOk || !loadOk) { // Se o Profile atual não tem as variáveis Local.*...
            log.AppendLine($"AVISO: não consegui definir Build/Load Path Local em '{nome}'; ajuste no Inspector (Schemas > Content Packing & Loading)."); // ...avisa para ajustar à mão.
        } // fim do if
        EditorUtility.SetDirty(schema); // Marca o schema como alterado para ser gravado em disco.
        EditorUtility.SetDirty(grupo); // Marca o grupo como alterado para ser gravado em disco.
        log.AppendLine($"INFO: grupo '{nome}' criado (Pack Separately, caminhos Local)."); // Registra a criação no resumo.
        return grupo; // Devolve o grupo novo.
    } // fim de ObterOuCriarGrupo

    private static bool EscreverGuid(SerializedProperty referencia, string guid) { // Escreve o GUID em um AssetReference; retorna true se algo mudou.
        SerializedProperty campoGuid = referencia.FindPropertyRelative(CampoGuid); // Localiza o campo interno m_AssetGUID.
        if (campoGuid.stringValue == guid) return false; // Já está correto: não faz nada (idempotência).
        campoGuid.stringValue = guid; // Grava o novo GUID.
        SerializedProperty subNome = referencia.FindPropertyRelative("m_SubObjectName"); // Campo do sub-objeto (usado só em sprites/sub-assets).
        if (subNome != null) subNome.stringValue = string.Empty; // Limpa: aqui a referência aponta para o asset principal.
        SerializedProperty subTipo = referencia.FindPropertyRelative("m_SubObjectType"); // Campo do tipo do sub-objeto.
        if (subTipo != null) subTipo.stringValue = string.Empty; // Limpa pelo mesmo motivo.
        return true; // Informa que houve mudança.
    } // fim de EscreverGuid

    private static void AnexarResumo(StringBuilder sb, string titulo, AddressableAssetGroup grupo, Contagem c) { // Escreve as linhas do resumo de uma categoria.
        sb.AppendLine($"{titulo}: marcados agora = {c.Novos} | movidos de outro grupo = {c.Movidos} | já estavam no grupo = {c.JaEstavam} | address ajustado = {c.EnderecosAjustados}"); // Linha 1: situação dos assets.
        sb.AppendLine($"         compartilhados = {c.Compartilhados} | ignorados = {c.Ignorados} | sem referência direta = {c.SemReferenciaDireta}"); // Linha 2: casos especiais.
        sb.AppendLine($"         Ref preenchidas/corrigidas = {c.RefsPreenchidas} | Ref já corretas = {c.RefsJaCorretas} | total no grupo '{grupo.Name}' = {grupo.entries.Count}"); // Linha 3: referências e total real no grupo.
    } // fim de AnexarResumo

    // ------------------------------------------------------------------ ETAPA 2: LIMPAR

    [MenuItem(MenuLimpar, false, 2)] // Cria o item de menu da etapa 2.
    public static void Limpar() { // Zera ModelPrefab/VoiceClip, mas só se todos os ModelRef/VoiceRef estiverem válidos.
        if (!PodeRodar(out List<SerializedObject> objetos)) return; // Aborta se estiver em Play Mode ou se não houver controller na cena.
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false); // Lê as configurações SEM criar (limpar não deve criar nada).
        if (settings == null) { // Sem configurações, a etapa 1 nunca rodou.
            Erro("O Addressables ainda não está configurado. Rode primeiro o item '1 - Migrar personagens'."); // Orienta o usuário.
            return; // Encerra sem alterar nada.
        } // fim do if

        Plano plano = new Plano(); // Guarda o resultado da verificação.
        foreach (SerializedObject so in objetos) { // Para cada controller encontrado...
            so.Update(); // ...sincroniza o SerializedObject com o componente...
            SerializedProperty lista = so.FindProperty("characters"); // ...localiza o array de personagens...
            for (int i = 0; i < lista.arraySize; i++) { // ...e examina um por um.
                SerializedProperty item = lista.GetArrayElementAtIndex(i); // Item i.
                string nome = NomeDoItem(item, i); // Nome legível para as mensagens.
                if (Validar(item.FindPropertyRelative("ModelPrefab"), item.FindPropertyRelative("ModelRef"), "ModelRef", GrupoModelos, true, nome, settings, plano.Problemas)) plano.Modelos++; // Verifica o modelo (obrigatório) e conta se ainda há algo a zerar.
                if (Validar(item.FindPropertyRelative("VoiceClip"), item.FindPropertyRelative("VoiceRef"), "VoiceRef", GrupoVozes, false, nome, settings, plano.Problemas)) plano.Vozes++; // Verifica a voz (opcional) e conta se ainda há algo a zerar.
            } // fim do for
        } // fim do foreach

        if (plano.Problemas.Count > 0) { // Se algum ModelRef/VoiceRef não está pronto...
            Debug.LogError(Prefixo + "Limpeza BLOQUEADA (nada foi alterado). Problemas:\n - " + string.Join("\n - ", plano.Problemas)); // ...lista todos no Console...
            EditorUtility.DisplayDialog("UmaTank - Addressables", plano.Problemas.Count + " problema(s) impedem a limpeza. Nada foi alterado.\nVeja a lista no Console e rode o item 1 novamente.", "OK"); // ...e avisa em uma janela.
            return; // Encerra sem zerar nada.
        } // fim do if

        if (plano.Modelos == 0 && plano.Vozes == 0) { // Nada para zerar: já foi limpo antes.
            Debug.Log(Prefixo + "Nada a limpar: ModelPrefab/VoiceClip já estão vazios e os ModelRef/VoiceRef estão válidos."); // Informa (isto é a idempotência da etapa 2).
            return; // Encerra sem tocar na cena.
        } // fim do if

        bool confirmou = EditorUtility.DisplayDialog("UmaTank - Limpar referências diretas", "Todos os ModelRef/VoiceRef estão preenchidos e válidos.\n\nIsto vai zerar " + plano.Modelos + " ModelPrefab e " + plano.Vozes + " VoiceClip na cena.\n\nATENÇÃO: o código atual (CreateEnemyRoster, ShowCharacter, BattleSessionController.ApplyCharacter etc.) ainda lê esses campos e só passa a usar ModelRef/VoiceRef no Passo 5. Faça um commit antes.\n\nContinuar?", "Limpar", "Cancelar"); // Pede confirmação explícita antes de apagar os dados.
        if (!confirmou) return; // Se cancelou, encerra sem alterar nada.

        Undo.IncrementCurrentGroup(); // Abre um novo grupo de Undo.
        Undo.SetCurrentGroupName(UndoLimpar); // Dá nome ao grupo de Undo.
        int grupoUndo = Undo.GetCurrentGroup(); // Guarda o índice para juntar tudo em um único Ctrl+Z.
        foreach (SerializedObject so in objetos) { // Para cada controller...
            SerializedProperty lista = so.FindProperty("characters"); // ...localiza o array de personagens...
            for (int i = 0; i < lista.arraySize; i++) { // ...e percorre os itens.
                SerializedProperty item = lista.GetArrayElementAtIndex(i); // Item i.
                item.FindPropertyRelative("ModelPrefab").objectReferenceValue = null; // Zera a referência direta ao prefab (o ModelRef continua).
                item.FindPropertyRelative("VoiceClip").objectReferenceValue = null; // Zera a referência direta ao clipe (o VoiceRef continua).
            } // fim do for
            if (so.ApplyModifiedProperties()) { // Grava no componente (registra Undo); retorna true só se algo mudou.
                EditorSceneManager.MarkSceneDirty(((Component)so.targetObject).gameObject.scene); // Marca a cena como modificada.
            } // fim do if
        } // fim do foreach
        Undo.CollapseUndoOperations(grupoUndo); // Junta tudo em um único passo de Undo.

        Debug.Log(Prefixo + "Limpeza concluída: " + plano.Modelos + " ModelPrefab e " + plano.Vozes + " VoiceClip zerados. Salve a cena (Ctrl+S). Lembrete: até o Passo 5, o jogo não consegue instanciar personagens (o código ainda lê ModelPrefab/VoiceClip)."); // Resumo final.
    } // fim de Limpar

    private static bool Validar(SerializedProperty direto, SerializedProperty referencia, string rotulo, string grupoEsperado, bool obrigatorio, string nome, AddressableAssetSettings settings, List<string> problemas) { // Confere um par direto/Ref; retorna true se ainda há referência direta a zerar.
        string guidRef = referencia.FindPropertyRelative(CampoGuid).stringValue; // GUID guardado em ModelRef/VoiceRef.
        Object asset = direto.objectReferenceValue; // Referência direta atual (pode já estar nula).

        if (string.IsNullOrEmpty(guidRef)) { // A referência Addressable está vazia.
            if (asset != null || obrigatorio) { // Vazia é erro se havia asset direto, ou se o campo é obrigatório (modelo).
                problemas.Add($"{nome}: {rotulo} está vazio."); // Registra o problema.
            } // fim do if
            return false; // Sem Ref válido não há o que limpar (e, se houve erro, ele já foi registrado).
        } // fim do if (guidRef vazio)

        if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guidRef))) { // O GUID não corresponde a nenhum asset do projeto.
            problemas.Add($"{nome}: {rotulo} aponta para um GUID que não existe mais ({guidRef})."); // Registra o problema.
            return asset != null; // Ainda há referência direta a zerar (mas a limpeza ficará bloqueada).
        } // fim do if

        AddressableAssetEntry entrada = settings.FindAssetEntry(guidRef); // Procura a entrada Addressable desse GUID.
        if (entrada == null) { // O asset existe, mas não está marcado como Addressable.
            problemas.Add($"{nome}: o asset de {rotulo} não está marcado como Addressable."); // Registra o problema.
        } else if (entrada.parentGroup != null && entrada.parentGroup.Name != grupoEsperado) { // Está Addressable, mas em outro grupo.
            Debug.LogWarning(Prefixo + $"{nome}: o asset de {rotulo} está no grupo '{entrada.parentGroup.Name}' (esperado '{grupoEsperado}'). Não bloqueia a limpeza."); // Apenas avisa.
        } // fim do if/else

        if (asset != null) { // Ainda existe referência direta: compara com o Ref para não perder dados.
            string guidDireto = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)); // GUID do asset ligado diretamente.
            if (guidDireto != guidRef) { // Os dois apontam para assets diferentes.
                problemas.Add($"{nome}: {rotulo} aponta para um asset diferente da referência direta. Rode o item 1 para corrigir antes de limpar."); // Registra o problema.
            } // fim do if
        } // fim do if

        return asset != null; // True = ainda há referência direta que a limpeza vai zerar.
    } // fim de Validar

    // ------------------------------------------------------------------ AUXILIARES

    private static bool PodeRodar(out List<SerializedObject> objetos) { // Verifica as condições comuns às duas etapas e prepara os SerializedObjects.
        objetos = new List<SerializedObject>(); // Lista de saída (vazia até o final).
        if (EditorApplication.isPlayingOrWillChangePlaymode) { // Alterações feitas em Play Mode seriam descartadas ao sair.
            Erro("Saia do Play Mode antes de rodar esta ferramenta."); // Avisa o usuário.
            return false; // Não pode rodar.
        } // fim do if

        List<CharacterSelectionController> controllers = LocalizarControllers(); // Procura o controller nas cenas abertas.
        if (controllers.Count == 0) { // Nenhum encontrado.
            Erro("Nenhum CharacterSelectionController nas cenas abertas. Abra Assets/Scenes/UmaTank.unity e tente de novo."); // Orienta o usuário.
            return false; // Não pode rodar.
        } // fim do if

        foreach (CharacterSelectionController controller in controllers) { // Para cada controller encontrado...
            SerializedObject so = new SerializedObject(controller); // ...cria o SerializedObject (as edições passam a registrar Undo).
            SerializedProperty lista = so.FindProperty("characters"); // ...localiza o array de personagens.
            bool camposOk = lista != null && lista.isArray && (lista.arraySize == 0 || TemCamposRef(lista.GetArrayElementAtIndex(0))); // O array existe e os campos novos estão presentes?
            if (!camposOk) { // Se faltam campos (script antigo, ainda compilando, ou Addressables ausente)...
                Erro("Os campos ModelRef/VoiceRef não foram encontrados em CharacterOption. Confira se o CharacterSelectionController.cs novo foi importado e compilou sem erros (e se o pacote Addressables está instalado)."); // ...explica o que conferir.
                return false; // Não pode rodar.
            } // fim do if
            objetos.Add(so); // Guarda o SerializedObject pronto para uso.
        } // fim do foreach
        return true; // Tudo certo.
    } // fim de PodeRodar

    private static bool TemCamposRef(SerializedProperty item) { // Confere se um CharacterOption tem ModelRef/VoiceRef com o campo m_AssetGUID.
        SerializedProperty modelo = item.FindPropertyRelative("ModelRef"); // Localiza ModelRef.
        SerializedProperty voz = item.FindPropertyRelative("VoiceRef"); // Localiza VoiceRef.
        return modelo != null && voz != null && modelo.FindPropertyRelative(CampoGuid) != null && voz.FindPropertyRelative(CampoGuid) != null; // True só se os quatro existem.
    } // fim de TemCamposRef

    private static List<CharacterSelectionController> LocalizarControllers() { // Varre as cenas abertas em busca do controller (inclusive objetos desativados).
        List<CharacterSelectionController> resultado = new List<CharacterSelectionController>(); // Lista de saída.
        for (int i = 0; i < SceneManager.sceneCount; i++) { // Para cada cena aberta...
            Scene cena = SceneManager.GetSceneAt(i); // ...pega a cena...
            if (!cena.isLoaded) continue; // ...ignora as que não estão carregadas...
            foreach (GameObject raiz in cena.GetRootGameObjects()) { // ...e percorre os objetos raiz...
                resultado.AddRange(raiz.GetComponentsInChildren<CharacterSelectionController>(true)); // ...coletando o componente (true = inclui desativados).
            } // fim do foreach
        } // fim do for
        return resultado; // Devolve o que foi encontrado.
    } // fim de LocalizarControllers

    private static string NomeDoItem(SerializedProperty item, int indice) { // Texto legível do item para as mensagens, ex.: "[8] Super Creek (Shinobi)".
        string nome = item.FindPropertyRelative("DisplayName").stringValue; // Nome de exibição.
        string aparencia = item.FindPropertyRelative("AppearanceName").stringValue; // Nome da aparência (pode estar vazio).
        return $"[{indice}] {nome}" + (string.IsNullOrWhiteSpace(aparencia) ? "" : $" ({aparencia})"); // Junta índice, nome e aparência (se houver).
    } // fim de NomeDoItem

    private static string IdSeguro(SerializedProperty item, int indice) { // CharacterId pronto para uso em um address.
        string id = item.FindPropertyRelative("CharacterId").stringValue; // Lê o CharacterId.
        if (string.IsNullOrWhiteSpace(id)) { // Se estiver vazio...
            id = item.FindPropertyRelative("DisplayName").stringValue.Trim().ToLowerInvariant().Replace(' ', '-'); // ...usa o DisplayName em minúsculas com hífens.
        } // fim do if
        if (string.IsNullOrWhiteSpace(id)) id = "indice-" + indice; // Último recurso: usa o índice do item.
        return id.Trim().Replace('/', '-'); // Remove espaços nas pontas e troca "/" (que quebraria a hierarquia do address).
    } // fim de IdSeguro

    private static string AparenciaSegura(SerializedProperty item) { // AppearanceName pronto para uso em um address.
        string aparencia = item.FindPropertyRelative("AppearanceName").stringValue; // Lê o AppearanceName.
        return string.IsNullOrWhiteSpace(aparencia) ? "default" : aparencia.Trim().Replace('/', '-'); // Vazio vira "default" (evita "char/id//model").
    } // fim de AparenciaSegura

    private static void Erro(string mensagem) { // Mostra um erro no Console e em uma janela.
        Debug.LogError(Prefixo + mensagem); // Registra no Console.
        EditorUtility.DisplayDialog("UmaTank - Addressables", mensagem, "OK"); // Mostra a janela de aviso.
    } // fim de Erro
} // fim da classe AddressablesMigrationTool
