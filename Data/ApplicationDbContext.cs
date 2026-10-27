using Microsoft.EntityFrameworkCore;
using SistemaWhiteLabel.API.Models;
using SistemaWhiteLabel.API.Modules.Fiado;

namespace SistemaWhiteLabel.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Pessoa> Pessoas { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Perfil> Perfis { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Venda> Vendas { get; set; }
        public DbSet<ItemVenda> ItensVenda { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<FormaPagamento> FormaPagamentos { get; set; }
        public DbSet<TipoPagamento> TiposPagamento { get; set; }
        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<FiadoVenda> Fiados { get; set; }
        public DbSet<Dispositivo> Dispositivos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("PIM");

            // ====================== PESSOA ======================
            modelBuilder.Entity<Pessoa>(entity =>
            {
                entity.ToTable("tb_pessoa");
                entity.HasKey(p => p.IdPessoa);
                entity.Property(p => p.IdPessoa).HasColumnName("id_pessoa");
                entity.Property(p => p.Nome).HasColumnName("nome_pessoa").IsRequired().HasMaxLength(100);
                entity.Property(p => p.Cpf).HasColumnName("cpf_pessoa").IsRequired().HasMaxLength(11);
                entity.Property(p => p.Telefone).HasColumnName("telefone_pessoa").HasMaxLength(15);
                entity.Property(p => p.Observacao).HasColumnName("observacao_pessoa").HasMaxLength(500);
            });

            // ====================== ENDERECO ======================
            modelBuilder.Entity<Endereco>(entity =>
            {
                entity.ToTable("tb_endereco");
                entity.HasKey(e => e.IdEndereco);
                entity.Property(e => e.IdEndereco).HasColumnName("id_endereco");
                entity.Property(e => e.Logradouro).HasColumnName("logradouro").IsRequired().HasMaxLength(150);
                entity.Property(e => e.Numero).HasColumnName("numero");
                entity.Property(e => e.Bairro).HasColumnName("bairro").HasMaxLength(100);
                entity.Property(e => e.Cep).HasColumnName("cep").HasMaxLength(9);
                entity.Property(e => e.IdPessoa).HasColumnName("id_pessoa");
                entity.Property(e => e.IdMunicipio).HasColumnName("id_municipio");

                entity.HasOne(e => e.Pessoa)
                    .WithMany(p => p.Enderecos)
                    .HasForeignKey(e => e.IdPessoa);
            });

            // ====================== FUNCIONARIO ======================
            // Extensão 1:1 de Pessoa: a chave primária é o próprio Id_Pessoa
            modelBuilder.Entity<Funcionario>(entity =>
            {
                entity.ToTable("tb_funcionario");
                entity.HasKey(f => f.IdPessoa);
                entity.Property(f => f.IdPessoa).HasColumnName("id_pessoa").ValueGeneratedNever();
                entity.Property(f => f.Cargo).HasColumnName("cargo").IsRequired().HasMaxLength(50);
                entity.Property(f => f.Salario).HasColumnName("salario").HasPrecision(12, 2);

                entity.HasOne(f => f.Pessoa)
                    .WithOne(p => p.Funcionario)
                    .HasForeignKey<Funcionario>(f => f.IdPessoa);
            });

            // ====================== PERFIL ======================
            modelBuilder.Entity<Perfil>(entity =>
            {
                entity.ToTable("tb_perfil");
                entity.HasKey(p => p.IdPerfil);
                entity.Property(p => p.IdPerfil).HasColumnName("id_perfil");
                entity.Property(p => p.Nome).HasColumnName("nome_perfil").IsRequired().HasMaxLength(50);
                entity.Property(p => p.Descricao).HasColumnName("descricao_perfil").HasMaxLength(200);
            });

            // ====================== CATEGORIA ======================
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("tb_categoria");
                entity.HasKey(c => c.IdCategoria);
                entity.Property(c => c.IdCategoria).HasColumnName("id_categoria");
                entity.Property(c => c.Descricao).HasColumnName("descricao").IsRequired().HasMaxLength(500);
            });

            // ====================== PRODUTO ======================
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.ToTable("tb_produto");
                entity.HasKey(p => p.IdProduto);
                entity.Property(p => p.IdProduto).HasColumnName("id_produto");
                entity.Property(p => p.Descricao).HasColumnName("descricao_produto").IsRequired().HasMaxLength(500);
                entity.Property(p => p.PrecoCompra).HasColumnName("preco_compra_produto").HasPrecision(12, 2);
                entity.Property(p => p.PrecoVenda).HasColumnName("preco_venda_produto").HasPrecision(12, 2);
                entity.Property(p => p.Qtde).HasColumnName("qdte_produto");
                entity.Property(p => p.EstoqueMinimo).HasColumnName("estoque_minimo_produto");
                entity.Property(p => p.IdCategoria).HasColumnName("id_categoria");
                entity.Property(p => p.IdUnidadeMedida).HasColumnName("id_unidade_medida");
            });

            // ====================== USUARIO ======================
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("tb_usuario");
                entity.HasKey(u => u.IdUsuario);
                entity.Property(u => u.IdUsuario).HasColumnName("id_usuario");
                entity.Property(u => u.Username).HasColumnName("username");
                entity.Property(u => u.Senha).HasColumnName("senha");
                entity.Property(u => u.Status).HasColumnName("status");
                entity.Property(u => u.Chave2FA).HasColumnName("chave_2fa");
                entity.Property(u => u.IdPessoa).HasColumnName("id_pessoa");
                entity.Property(u => u.IdPerfil).HasColumnName("id_perfil");

                entity.HasOne(u => u.Pessoa)
                    .WithOne(p => p.Usuario)
                    .HasForeignKey<Usuario>(u => u.IdPessoa);

                entity.HasOne(u => u.Perfil)
                    .WithMany()
                    .HasForeignKey(u => u.IdPerfil);
            });

            // ====================== VENDA ======================
            modelBuilder.Entity<Venda>(entity =>
            {
                entity.ToTable("tb_venda");
                entity.HasKey(v => v.IdVenda);
                entity.Property(v => v.IdVenda).HasColumnName("id_venda");
                entity.Property(v => v.DataHora).HasColumnName("data_hora_venda");
                entity.Property(v => v.TotalVenda).HasColumnName("total_venda").HasPrecision(12, 2);
                entity.Property(v => v.TotalDesconto).HasColumnName("total_desconto_venda").HasPrecision(12, 2);
                entity.Property(v => v.Status).HasColumnName("status_venda_venda").IsRequired().HasMaxLength(15);
                entity.Property(v => v.IdPessoa).HasColumnName("id_pessoa");
                entity.Property(v => v.IdUsuario).HasColumnName("id_usuario");

                entity.HasOne(v => v.Pessoa)
                    .WithMany()
                    .HasForeignKey(v => v.IdPessoa)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Usuario)
                    .WithMany()
                    .HasForeignKey(v => v.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(v => v.Itens)
                    .WithOne(i => i.Venda)
                    .HasForeignKey(i => i.IdVenda);

                entity.HasMany(v => v.Pagamentos)
                    .WithOne(f => f.Venda)
                    .HasForeignKey(f => f.IdVenda)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ====================== ITEM_VENDA ======================
            modelBuilder.Entity<ItemVenda>(entity =>
            {
                entity.ToTable("tb_item_venda");
                entity.HasKey(i => i.IdItemVenda);
                entity.Property(i => i.IdItemVenda).HasColumnName("id_item_venda");
                entity.Property(i => i.IdVenda).HasColumnName("id_venda");
                entity.Property(i => i.IdProduto).HasColumnName("id_produto");
                entity.Property(i => i.Qdte).HasColumnName("qdte_item_venda");
                entity.Property(i => i.PrecoUnitario).HasColumnName("preco_unitario_item_venda").HasPrecision(12, 2);
                entity.Property(i => i.ValorDesconto).HasColumnName("valor_desconto_item_venda").HasPrecision(12, 2);
                entity.Property(i => i.ValorTotal).HasColumnName("valor_total_item_venda").HasPrecision(12, 2);
            });

            // ====================== FORMA_PAGAMENTO ======================
            modelBuilder.Entity<FormaPagamento>(entity =>
            {
                entity.ToTable("tb_forma_pagamento");
                entity.HasKey(f => f.IdFormaPgto);
                entity.Property(f => f.IdFormaPgto).HasColumnName("id_forma_pagamento");
                entity.Property(f => f.DataHoraPgto).HasColumnName("data_hora_pagamento");
                entity.Property(f => f.TotalPgto).HasColumnName("total_forma_pagamento").HasPrecision(12, 2);
                entity.Property(f => f.IdVenda).HasColumnName("id_venda");
                entity.Property(f => f.IdTipoPgto).HasColumnName("id_tipo_pagamento");

                entity.HasOne(f => f.TipoPagamento)
                    .WithMany()
                    .HasForeignKey(f => f.IdTipoPgto);
            });

            // ====================== TIPO_PAGAMENTO ======================
            modelBuilder.Entity<TipoPagamento>(entity =>
            {
                entity.ToTable("tb_tipo_pagamento");
                entity.HasKey(t => t.IdTipoPgto);
                entity.Property(t => t.IdTipoPgto).HasColumnName("id_tipo_pagamento");
                entity.Property(t => t.Descricao).HasColumnName("descricao").IsRequired().HasMaxLength(50);
            });

            // ====================== MODULO ======================
            modelBuilder.Entity<Modulo>(entity =>
            {
                entity.ToTable("tb_modulo");
                entity.HasKey(m => m.Nome);
                entity.Property(m => m.Nome).HasColumnName("nome_modulo").HasMaxLength(50);
                entity.Property(m => m.Descricao).HasColumnName("descricao_modulo").HasMaxLength(200);
                entity.Property(m => m.Ativo).HasColumnName("ativo_modulo").IsRequired().HasMaxLength(1);
            });

            // ====================== FIADO (módulo Fiado) ======================
            modelBuilder.Entity<FiadoVenda>(entity =>
            {
                entity.ToTable("tb_fiado");
                entity.HasKey(f => f.IdFiado);
                entity.Property(f => f.IdFiado).HasColumnName("id_fiado");
                entity.Property(f => f.IdVenda).HasColumnName("id_venda");
                entity.Property(f => f.IdPessoa).HasColumnName("id_pessoa");
                entity.Property(f => f.ValorOriginal).HasColumnName("valor_original").HasPrecision(12, 2);
                entity.Property(f => f.DataHora).HasColumnName("data_hora_fiado");

                entity.HasOne(f => f.Venda)
                    .WithMany()
                    .HasForeignKey(f => f.IdVenda)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Pessoa)
                    .WithMany()
                    .HasForeignKey(f => f.IdPessoa)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ====================== DISPOSITIVO (celular autorizado pelo PC) ======================
            modelBuilder.Entity<Dispositivo>(entity =>
            {
                entity.ToTable("tb_dispositivo");
                entity.HasKey(d => d.IdDispositivo);
                entity.Property(d => d.IdDispositivo).HasColumnName("id_dispositivo");
                entity.Property(d => d.IdUsuario).HasColumnName("id_usuario");
                entity.Property(d => d.Nome).HasColumnName("nome_dispositivo").IsRequired().HasMaxLength(100);
                entity.Property(d => d.CriadoEm).HasColumnName("criado_em");
                entity.Property(d => d.ExpiraEm).HasColumnName("expira_em");
                entity.Property(d => d.LimiteEm).HasColumnName("limite_em");
                entity.Property(d => d.UltimoUso).HasColumnName("ultimo_uso");
                entity.Property(d => d.RevogadoEm).HasColumnName("revogado_em");

                entity.HasOne(d => d.Usuario)
                    .WithMany()
                    .HasForeignKey(d => d.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
