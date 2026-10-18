-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Tempo de geração: 18/10/2026 às 22:27
-- Versão do servidor: 10.4.32-MariaDB
-- Versão do PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Banco de dados: `tank_gamers`
--

-- --------------------------------------------------------

--
-- Estrutura para tabela `jogadores`
--

CREATE TABLE `jogadores` (
  `id` int(10) UNSIGNED NOT NULL,
  `name` varchar(200) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password` varchar(200) NOT NULL,
  `pontos` int(11) NOT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

--
-- Despejando dados para a tabela `jogadores`
--

INSERT INTO `jogadores` (`id`, `name`, `email`, `password`, `pontos`, `created_at`, `updated_at`) VALUES
(1, 'Eduardo2', 'edu@edu.com', '$2y$10$b7vTBDE1isHlpy681YQ0aOv4rlTCx9n6P1aNmhqhz1buNywZ2OG16', 0, '2026-10-02 20:45:47', '2026-10-18 17:35:35');

-- --------------------------------------------------------

--
-- Estrutura para tabela `partidas`
--

CREATE TABLE `partidas` (
  `id` int(10) UNSIGNED NOT NULL,
  `jogador_id` int(10) UNSIGNED NOT NULL,
  `partida_uuid` char(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `inicio_utc` datetime(3) NOT NULL,
  `fim_utc` datetime(3) NOT NULL,
  `duracao_segundos` decimal(10,3) NOT NULL,
  `resultado` enum('vitoria','derrota','empate') NOT NULL,
  `pontuacao` int(10) UNSIGNED NOT NULL,
  `inimigos_eliminados` int(10) UNSIGNED NOT NULL
) ;

--
-- Despejando dados para a tabela `partidas`
--

INSERT INTO `partidas` (`id`, `jogador_id`, `partida_uuid`, `inicio_utc`, `fim_utc`, `duracao_segundos`, `resultado`, `pontuacao`, `inimigos_eliminados`) VALUES
(1, 1, '9504b698-4c05-44ad-b073-e8ce529df711', '2026-10-18 17:52:49.000', '2026-10-18 17:52:59.000', 10.548, 'derrota', 0, 0),
(2, 1, '0634af7d-9743-4ceb-a981-c2adbd30c6eb', '2026-10-18 20:15:29.000', '2026-10-18 20:15:49.000', 18.983, 'derrota', 0, 0);

-- --------------------------------------------------------

--
-- Estrutura para tabela `tokens`
--

CREATE TABLE `tokens` (
  `id` int(10) UNSIGNED NOT NULL,
  `jogador_id` int(10) UNSIGNED NOT NULL,
  `token` varchar(200) NOT NULL,
  `updated_at` datetime NOT NULL,
  `expiration_at` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Despejando dados para a tabela `tokens`
--

INSERT INTO `tokens` (`id`, `jogador_id`, `token`, `updated_at`, `expiration_at`) VALUES
(1, 1, 'd1fdd5d4086f46529b60d1ece00dbd50', '2026-10-18 17:15:20', '2026-10-19 17:15:20');

--
-- Índices para tabelas despejadas
--

--
-- Índices de tabela `jogadores`
--
ALTER TABLE `jogadores`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_jogadores_email` (`email`);

--
-- Índices de tabela `partidas`
--
ALTER TABLE `partidas`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_partida_jogador` (`jogador_id`,`partida_uuid`),
  ADD KEY `idx_partidas_ranking` (`resultado`,`jogador_id`,`pontuacao`),
  ADD KEY `idx_partidas_historico` (`jogador_id`,`inicio_utc`);

--
-- Índices de tabela `tokens`
--
ALTER TABLE `tokens`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `jogador_id` (`jogador_id`);

--
-- AUTO_INCREMENT para tabelas despejadas
--

--
-- AUTO_INCREMENT de tabela `jogadores`
--
ALTER TABLE `jogadores`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT de tabela `partidas`
--
ALTER TABLE `partidas`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de tabela `tokens`
--
ALTER TABLE `tokens`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- Restrições para tabelas despejadas
--

--
-- Restrições para tabelas `partidas`
--
ALTER TABLE `partidas`
  ADD CONSTRAINT `fk_partidas_jogador` FOREIGN KEY (`jogador_id`) REFERENCES `jogadores` (`id`);

--
-- Restrições para tabelas `tokens`
--
ALTER TABLE `tokens`
  ADD CONSTRAINT `fk_tokens_jogador` FOREIGN KEY (`jogador_id`) REFERENCES `jogadores` (`id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
