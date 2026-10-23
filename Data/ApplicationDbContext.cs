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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("PIM");

            // ====================== PESSOA ======================
            modelBuilder.Entity<Pessoa>(entity =>
            {
                entity.ToTable("tbl_Pessoa");
                entity.HasKey(p => p.IdPessoa);
                entity.Property(p => p.IdPessoa).HasColumnName("Id_Pessoa");
                entity.Property(p => p.Nome).HasColumnName("Nome_Pessoa").IsRequired().HasMaxLength(100);
                entity.Property(p => p.Cpf).HasColumnName("CPF_Pessoa").IsRequired().HasMaxLength(11);
                entity.Property(p => p.Telefone).HasColumnName("Telefone_Pessoa").HasMaxLength(15);
                entity.Property(p => p.Observacao).HasColumnName("Observacao_Pessoa").HasMaxLength(500);
            });

            // ====================== ENDERECO ======================
            modelBuilder.Entity<Endereco>(entity =>
            {
                entity.ToTable("tbl_Endereco");
                entity.HasKey(e => e.IdEndereco);
                entity.Property(e => e.IdEndereco).HasColumnName("Id_Endereco");
                entity.Property(e => e.Logradouro).HasColumnName("Logradouro").IsRequired().HasMaxLength(150);
                entity.Property(e => e.Numero).HasColumnName("Numero");
                entity.Property(e => e.Bairro).HasColumnName("Bairro").HasMaxLength(100);
                entity.Property(e => e.Cep).HasColumnName("CEP").HasMaxLength(9);
                entity.Property(e => e.IdPessoa).HasColumnName("Id_Pessoa");
                entity.Property(e => e.IdMunicipio).HasColumnName("Id_Municipio");

                entity.HasOne(e => e.Pessoa)
                    .WithMany(p => p.Enderecos)
                    .HasForeignKey(e => e.IdPessoa);
            });

            // ====================== FUNCIONARIO ======================
            // Extensão 1:1 de Pessoa: a chave primária é o próprio Id_Pessoa
            modelBuilder.Entity<Funcionario>(entity =>
            {
                entity.ToTable("tbl_Funcionario");
                entity.HasKey(f => f.IdPessoa);
                entity.Property(f => f.IdPessoa).HasColumnName("Id_Pessoa").ValueGeneratedNever();
                entity.Property(f => f.Cargo).HasColumnName("Cargo").IsRequired().HasMaxLength(50);
                entity.Property(f => f.Salario).HasColumnName("Salario").HasPrecision(12, 2);

                entity.HasOne(f => f.Pessoa)
                    .WithOne(p => p.Funcionario)
                    .HasForeignKey<Funcionario>(f => f.IdPessoa);
            });

            // ====================== PERFIL ======================
            modelBuilder.Entity<Perfil>(entity =>
            {
                entity.ToTable("tbl_Perfil");
                entity.HasKey(p => p.IdPerfil);
                entity.Property(p => p.IdPerfil).HasColumnName("Id_Perfil");
                entity.Property(p => p.Nome).HasColumnName("Nome_Perfil").IsRequired().HasMaxLength(50);
                entity.Property(p => p.Descricao).HasColumnName("Descricao_Perfil").HasMaxLength(200);
            });

            // ====================== CATEGORIA ======================
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("tbl_Categoria");
                entity.HasKey(c => c.IdCategoria);
                entity.Property(c => c.IdCategoria).HasColumnName("Id_Categoria");
                entity.Property(c => c.Descricao).HasColumnName("Descricao").IsRequired().HasMaxLength(500);
            });

            // ====================== PRODUTO ======================
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.ToTable("tbl_Produto");
                entity.HasKey(p => p.IdProduto);
                entity.Property(p => p.IdProduto).HasColumnName("Id_Produto");
                entity.Property(p => p.Descricao).HasColumnName("Descricao_Produto").IsRequired().HasMaxLength(500);
                entity.Property(p => p.PrecoCompra).HasColumnName("Preco_Compra_Produto").HasPrecision(12, 2);
                entity.Property(p => p.PrecoVenda).HasColumnName("Preco_Venda_Produto").HasPrecision(12, 2);
                entity.Property(p => p.Qtde).HasColumnName("QDTE_Produto");
                entity.Property(p => p.EstoqueMinimo).HasColumnName("Estoque_Minimo_Produto");
                entity.Property(p => p.IdCategoria).HasColumnName("Id_Categoria");
                entity.Property(p => p.IdUnidadeMedida).HasColumnName("Id_Unidade_Medida");
            });

            // ====================== USUARIO ======================
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("tbl_Usuario");
                entity.HasKey(u => u.IdUsuario);
                entity.Property(u => u.IdUsuario).HasColumnName("Id_Usuario");
                entity.Property(u => u.Username).HasColumnName("UserName");
                entity.Property(u => u.Senha).HasColumnName("Senha");
                entity.Property(u => u.Status).HasColumnName("Status");
                entity.Property(u => u.Chave2FA).HasColumnName("Chave_2FA");
                entity.Property(u => u.IdPessoa).HasColumnName("Id_Pessoa");
                entity.Property(u => u.IdPerfil).HasColumnName("Id_Perfil");

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
                entity.ToTable("tbl_Venda");
                entity.HasKey(v => v.IdVenda);
                entity.Property(v => v.IdVenda).HasColumnName("Id_Venda");
                entity.Property(v => v.DataHora).HasColumnName("Data_Hora_Venda");
                entity.Property(v => v.TotalVenda).HasColumnName("Total_Venda").HasPrecision(12, 2);
                entity.Property(v => v.TotalDesconto).HasColumnName("Total_Desconto_Venda").HasPrecision(12, 2);
                entity.Property(v => v.Status).HasColumnName("Status_Venda_Venda").IsRequired().HasMaxLength(15);
                entity.Property(v => v.IdPessoa).HasColumnName("Id_Pessoa");
                entity.Property(v => v.IdUsuario).HasColumnName("Id_Usuario");

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
                entity.ToTable("tbl_Item_Venda");
                entity.HasKey(i => i.IdItemVenda);
                entity.Property(i => i.IdItemVenda).HasColumnName("Id_Item_Venda");
                entity.Property(i => i.IdVenda).HasColumnName("Id_Venda");
                entity.Property(i => i.IdProduto).HasColumnName("Id_Produto");
                entity.Property(i => i.Qdte).HasColumnName("QDTE_Item_Venda");
                entity.Property(i => i.PrecoUnitario).HasColumnName("Preco_Unitario_Item_Venda").HasPrecision(12, 2);
                entity.Property(i => i.ValorDesconto).HasColumnName("Valor_Desconto_Item_Venda").HasPrecision(12, 2);
                entity.Property(i => i.ValorTotal).HasColumnName("Valor_Total_Item_Venda").HasPrecision(12, 2);
            });

            // ====================== FORMA_PAGAMENTO ======================
            modelBuilder.Entity<FormaPagamento>(entity =>
            {
                entity.ToTable("tbl_Forma_Pagamento");
                entity.HasKey(f => f.IdFormaPgto);
                entity.Property(f => f.IdFormaPgto).HasColumnName("Id_Forma_Pagamento");
                entity.Property(f => f.DataHoraPgto).HasColumnName("Data_Hora_Pagamento");
                entity.Property(f => f.TotalPgto).HasColumnName("Total_Forma_Pagamento").HasPrecision(12, 2);
                entity.Property(f => f.IdVenda).HasColumnName("Id_Venda");
                entity.Property(f => f.IdTipoPgto).HasColumnName("Id_Tipo_Pagamento");

                entity.HasOne(f => f.TipoPagamento)
                    .WithMany()
                    .HasForeignKey(f => f.IdTipoPgto);
            });

            // ====================== TIPO_PAGAMENTO ======================
            modelBuilder.Entity<TipoPagamento>(entity =>
            {
                entity.ToTable("tbl_Tipo_Pagamento");
                entity.HasKey(t => t.IdTipoPgto);
                entity.Property(t => t.IdTipoPgto).HasColumnName("Id_Tipo_Pagamento");
                entity.Property(t => t.Descricao).HasColumnName("Descricao").IsRequired().HasMaxLength(50);
            });

            // ====================== MODULO ======================
            modelBuilder.Entity<Modulo>(entity =>
            {
                entity.ToTable("tbl_Modulo");
                entity.HasKey(m => m.Nome);
                entity.Property(m => m.Nome).HasColumnName("Nome_Modulo").HasMaxLength(50);
                entity.Property(m => m.Descricao).HasColumnName("Descricao_Modulo").HasMaxLength(200);
                entity.Property(m => m.Ativo).HasColumnName("Ativo_Modulo").IsRequired().HasMaxLength(1);
            });

            // ====================== FIADO (módulo Fiado) ======================
            modelBuilder.Entity<FiadoVenda>(entity =>
            {
                entity.ToTable("tbl_Fiado");
                entity.HasKey(f => f.IdFiado);
                entity.Property(f => f.IdFiado).HasColumnName("Id_Fiado");
                entity.Property(f => f.IdVenda).HasColumnName("Id_Venda");
                entity.Property(f => f.IdPessoa).HasColumnName("Id_Pessoa");
                entity.Property(f => f.ValorOriginal).HasColumnName("Valor_Original").HasPrecision(12, 2);
                entity.Property(f => f.DataHora).HasColumnName("Data_Hora_Fiado");

                entity.HasOne(f => f.Venda)
                    .WithMany()
                    .HasForeignKey(f => f.IdVenda)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Pessoa)
                    .WithMany()
                    .HasForeignKey(f => f.IdPessoa)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
