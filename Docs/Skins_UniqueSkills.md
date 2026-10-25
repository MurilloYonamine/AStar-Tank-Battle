# Unique Skills das aparências

Cada opção da seleção referencia seu próprio `CharacterClassDefinition`. São 25 aparências e 22 famílias de personagens. A identidade da personagem não muda: escolher a Doto normal ou Halloween exclui as duas do conjunto de inimigas.

Os efeitos abaixo são adaptações próprias para o UmaTank. Os nomes, números e condições não reproduzem literalmente as cartas do jogo original. “Bazuca” e “Xen” identificam os modelos disponíveis neste projeto, sem afirmar que sejam nomes de versões oficiais.

| Aparência | Unique Skill | Efeito no UmaTank |
| --- | --- | --- |
| Meisho Doto — Halloween | Travessura à meia-noite | Por 8 s: +35% de velocidade, −20% no intervalo de tiro e −10% de dano recebido. |
| Super Creek — Múmia | Abraço da múmia | Recupera 25% da vida máxima ao longo de 6 s; recebe 15% menos dano durante o efeito. |
| Super Creek — Shinobi | Caminho da sombra | Por 7 s: +60% de velocidade e −15% no intervalo de tiro. Não fica invisível nem atravessa obstáculos. |
| Mayano Top Gun — Halloween | Voo da bruxinha | Por 7 s: +40% de velocidade e −20% no intervalo de tiro. Não voa sobre obstáculos nem recupera vida. |
| Oguri Cap — Natal | Milagre da noite santa | Recupera 25% da vida máxima ao ativar; por 8 s, +20% de velocidade e −10% no intervalo de tiro. |
| Gold Ship — Bazuca | Surpresa de bazuca | Próximo tiro em até 10 s: +70% de dano e +100% de raio da explosão. |
| Yaeno Muteki — Xen | Impacto Xen | Próximo tiro em até 8 s: +60% de dano e +50% de raio da explosão. |

Os bônus temporários multiplicam os atributos da classe. A carga continua sendo 2 pontos por segundo de combate e 15 por eliminação, até 100. SHIFT ativa a habilidade. Não há carga durante o efeito. A pausa congela carga, duração e cura sem mudar `Time.timeScale`.

O tiro fortalecido é consumido ao disparar, mesmo que erre ou atinja um obstáculo. O projétil guarda os bônus daquele disparo; encerrar a habilidade não remove os bônus de um projétil já lançado. A imunidade ao próprio tiro foi mantida. Inimigas recebem atributos e passivas da aparência, mas não ativam Unique Skills.

## Onde ajustar

Os assets estão em `Assets/Settings/CharacterClasses`. Os novos são `DotoHalloweenClass`, `CreekShinobiClass` e `YaenoXenClass`. Na cena `Assets/Scenes/UmaTank.unity`, cada entrada de `CharacterSelectionController > Characters` aponta para seu perfil em **Class Definition**. As imagens continuam opcionais no campo **Unique Skill Icon** de cada perfil; as vozes permanecem nos campos já existentes da seleção.

Modelos, nomes de prefabs, mapa, grid, pontuação por tempo e API PHP não foram alterados. As referências das personagens incluem os perfis oficiais de [Meisho Doto](https://umamusume.jp/character/meishodoto/), [Super Creek](https://umamusume.jp/character/supercreek) e [Mayano Top Gun](https://umamusume.jp/character/mayanotopgun). Esses perfis não são documentação das habilidades de tanque descritas aqui.

## Validação de 24/10/2026

- Compilação dos scripts atuais: passou, incluindo a verificação dos comentários XML.
- Perfis: 25 classes/habilidades nomeadas e habilitadas, mantendo 22 famílias e as sete aparências com traje. Cada entrada aponta para seu próprio perfil.
- Cena: apenas três referências de classe foram trocadas; a comparação confirmou que geometria, modelos e vozes permaneceram iguais. As 103 verificações de referências passaram, cobrindo 670 dependências.
- Lógica, combate, classes e resultados: 559 verificações automatizadas passaram, incluindo carga, limites, duração, passivas, cura, dano e composição dos modificadores.
- Cópia isolada da cena integrada: 4.701 verificações passaram. Esse número inclui verificações por objeto/osso e não representa 4.701 partidas.
- Nas sete aparências: ativação por SHIFT, nome correto na HUD, velocidade e dano efetivos, cura limitada, pausa de carga/duração/cura, término sem acúmulo de bônus e exclusão da própria família do conjunto de inimigas. Gold Ship e Yaeno Xen também tiveram o disparo único e seus bônus no projétil verificados.
- Regressão do ciclo: seleção, contagem, câmera, quatro inimigas, movimento, Q/E, disparo, pausa sem `Time.timeScale = 0`, vitória/derrota/empate, confirmação do envio ao PHP isolado e volta ao menu.

Não houve exceções de gameplay nos cenários da cena integrada. O log ainda registra uma ocorrência de indexação interna do `UnityEditor.Search` na inicialização do Editor, anterior ao gameplay; não foi tratada como falha de habilidade nem corrigida nesta etapa.

O servidor de testes simula respostas e não acessa o MySQL real; nenhum resultado de teste foi inserido no banco do grupo. Não foi gerada nem validada uma nova build nesta etapa. Os testes automatizados não substituem uma rodada manual para avaliar o balanceamento e a sensação de cada habilidade.
