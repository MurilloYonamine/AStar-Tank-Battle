-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Tempo de geração: 2026-10-25 02:32:51
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
(1, 'Eduardo2', 'edu@edu.com', '$2y$10$b7vTBDE1isHlpy681YQ0aOv4rlTCx9n6P1aNmhqhz1buNywZ2OG16', 0, '2026-10-02 20:45:47', '2026-10-18 17:35:35'),
(6, 'Hugo', 'hugo@hugo.com', '$2y$10$SvXcsgeAL/1HjrWotEyZ2eYrLYkLowYr49J24.DDvs.oYtkF5OGvq', 0, '2026-10-24 19:02:12', '2026-10-24 19:02:12'),
(7, 'jorji', 'jorji@jorji.com', '$2y$10$y.kZXB4TwH7CmycmcE5gOeBuH9cKckzHuNZxCaMFZsCztTVYSnXt2', 0, '2026-10-24 20:22:19', '2026-10-24 20:22:19');

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

--
-- Despejando dados para a tabela `partidas`
--

INSERT INTO `partidas` (`id`, `jogador_id`, `partida_uuid`, `inicio_utc`, `fim_utc`, `duracao_segundos`, `resultado`, `pontuacao`, `inimigos_eliminados`) VALUES
(1, 1, '9504b698-4c05-44ad-b073-e8ce529df711', '2026-10-18 17:52:49.000', '2026-10-18 17:52:59.000', 10.548, 'derrota', 0, 0),
(2, 1, '0634af7d-9743-4ceb-a981-c2adbd30c6eb', '2026-10-18 20:15:29.000', '2026-10-18 20:15:49.000', 18.983, 'derrota', 0, 0),
(3, 6, 'a0fe397b-c4fd-4b2c-b32d-925cc499e10b', '2026-10-24 19:03:36.000', '2026-10-24 19:04:24.000', 46.449, 'derrota', 200, 2),
(4, 6, 'a3cfdf77-3598-4880-9130-443c85f6a890', '2026-10-24 19:04:57.000', '2026-10-24 19:06:01.000', 63.797, 'derrota', 800, 8),
(5, 6, 'ccc2723d-1c07-4d4b-93aa-8a07a8b73982', '2026-10-24 20:19:28.000', '2026-10-24 20:19:46.000', 18.546, 'derrota', 100, 1),
(6, 6, 'd1aa9e37-7790-4559-9934-bb7c93502c7f', '2026-10-24 20:19:59.000', '2026-10-24 20:22:00.000', 120.719, 'vitoria', 1000, 10),
(7, 7, 'fd2a77c8-771c-4dc9-a8be-f9477151f745', '2026-10-24 20:22:39.000', '2026-10-24 20:24:01.000', 82.141, 'derrota', 700, 7),
(8, 7, 'f992dc69-bbd2-461f-9241-50ea6edc83b2', '2026-10-24 20:24:14.000', '2026-10-24 20:24:55.000', 41.152, 'derrota', 200, 2),
(9, 7, 'cbae99ef-3543-4ec0-aa20-df85f7b10d37', '2026-10-24 20:25:07.000', '2026-10-24 20:26:01.000', 54.465, 'derrota', 400, 4),
(10, 7, '64382508-5a11-42ea-bddc-e920a120aa6e', '2026-10-24 20:26:15.000', '2026-10-24 20:27:47.000', 91.856, 'derrota', 400, 4),
(11, 7, 'e2f49789-b509-4a02-824c-6212bfefdb89', '2026-10-24 20:27:59.000', '2026-10-24 20:28:40.000', 41.503, 'derrota', 300, 3),
(12, 7, 'a0eae46f-6733-4a90-80fb-ad9e09a60816', '2026-10-24 20:28:53.000', '2026-10-24 20:30:18.000', 84.856, 'derrota', 700, 7),
(13, 7, '730ac740-c9e0-406a-9a1e-9f999c91d4b5', '2026-10-24 20:33:40.000', '2026-10-24 20:35:11.000', 91.131, 'derrota', 900, 9),
(14, 7, '74d27fc7-eadc-45a7-9231-ec9a3474e5e5', '2026-10-24 20:35:28.000', '2026-10-24 20:37:25.000', 116.833, 'vitoria', 1000, 10),
(15, 7, '212b01ee-14fb-4cb3-8bfb-dba17082f637', '2026-10-24 22:14:35.000', '2026-10-24 22:16:05.000', 90.527, 'vitoria', 0, 10);

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
(1, 1, 'd1fdd5d4086f46529b60d1ece00dbd50', '2026-10-18 17:15:20', '2026-10-19 17:15:20'),
(2, 6, 'a39e9996f2814b0e9e8082c91cc00c50', '2026-10-24 17:19:53', '2026-10-25 17:19:53'),
(3, 7, 'd07c781d108c484bb6f1babcde73b280', '2026-10-24 21:22:08', '2026-10-25 21:22:08');

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
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT de tabela `partidas`
--
ALTER TABLE `partidas`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=16;

--
-- AUTO_INCREMENT de tabela `tokens`
--
ALTER TABLE `tokens`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

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
