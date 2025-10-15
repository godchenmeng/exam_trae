using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Domain.Entities;

namespace ExamSystem.Infrastructure.Repositories
{
    /// <summary>
    /// 试卷Repository实现类
    /// </summary>
    public class ExamPaperRepository : BaseRepository<ExamPaper>, IExamPaperRepository
    {
        public ExamPaperRepository(ExamDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据创建者ID获取试卷列表
        /// </summary>
        public async Task<IEnumerable<ExamPaper>> GetPapersByCreatorIdAsync(int creatorId)
        {
            return await _dbSet.Where(p => p.CreatorId == creatorId)
                              .OrderByDescending(p => p.CreatedAt)
                              .ToListAsync();
        }

        /// <summary>
        /// 获取已发布的试卷列表
        /// </summary>
        public async Task<IEnumerable<ExamPaper>> GetPublishedPapersAsync()
        {
            return await _dbSet.Where(p => p.Status == "已发布")
                              .OrderByDescending(p => p.CreatedAt)
                              .ToListAsync();
        }

        /// <summary>
        /// 搜索试卷
        /// </summary>
        public async Task<IEnumerable<ExamPaper>> SearchPapersAsync(string keyword, int? creatorId = null, bool? isPublished = null)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) ||
                                        p.Description.Contains(keyword));
            }

            if (creatorId.HasValue)
            {
                query = query.Where(p => p.CreatorId == creatorId.Value);
            }

            if (isPublished.HasValue)
            {
                var status = isPublished.Value ? "已发布" : "草稿";
                query = query.Where(p => p.Status == status);
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        /// <summary>
        /// 获取试卷及其题目
        /// </summary>
        public async Task<ExamPaper?> GetPaperWithQuestionsAsync(int paperId)
        {
            return await _dbSet.Include(p => p.PaperQuestions)
                              .ThenInclude(pq => pq.Question)
                              .FirstOrDefaultAsync(p => p.PaperId == paperId);
        }

        /// <summary>
        /// 获取试卷的题目列表
        /// </summary>
        public async Task<IEnumerable<Question>> GetPaperQuestionsAsync(int paperId)
        {
            return await _context.PaperQuestions
                                .Where(pq => pq.PaperId == paperId)
                                .OrderBy(pq => pq.OrderIndex)
                                .Select(pq => pq.Question)
                                .ToListAsync();
        }

        /// <summary>
        /// 获取试卷的题目及选项
        /// </summary>
        public async Task<IEnumerable<Question>> GetPaperQuestionsWithOptionsAsync(int paperId)
        {
            return await _context.PaperQuestions
                                .Where(pq => pq.PaperId == paperId)
                                .OrderBy(pq => pq.OrderIndex)
                                .Select(pq => pq.Question)
                                .Include(q => q.Options)
                                .ToListAsync();
        }

        /// <summary>
        /// 添加题目到试卷
        /// </summary>
        public async Task AddQuestionToPaperAsync(int paperId, int questionId, decimal score, int sortOrder)
        {
            var paperQuestion = new PaperQuestion
            {
                PaperId = paperId,
                QuestionId = questionId,
                Score = score,
                OrderIndex = sortOrder
            };

            await _context.PaperQuestions.AddAsync(paperQuestion);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 从试卷中移除题目
        /// </summary>
        public async Task RemoveQuestionFromPaperAsync(int paperId, int questionId)
        {
            var paperQuestion = await _context.PaperQuestions
                                             .FirstOrDefaultAsync(pq => pq.PaperId == paperId && pq.QuestionId == questionId);
            if (paperQuestion != null)
            {
                _context.PaperQuestions.Remove(paperQuestion);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 更新试卷题目分值
        /// </summary>
        public async Task UpdateQuestionScoreAsync(int paperId, int questionId, decimal score)
        {
            var paperQuestion = await _context.PaperQuestions
                                             .FirstOrDefaultAsync(pq => pq.PaperId == paperId && pq.QuestionId == questionId);
            if (paperQuestion != null)
            {
                paperQuestion.Score = score;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 更新试卷题目排序
        /// </summary>
        public async Task UpdateQuestionSortOrderAsync(int paperId, int questionId, int sortOrder)
        {
            var paperQuestion = await _context.PaperQuestions
                                             .FirstOrDefaultAsync(pq => pq.PaperId == paperId && pq.QuestionId == questionId);
            if (paperQuestion != null)
            {
                paperQuestion.OrderIndex = sortOrder;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 复制试卷
        /// </summary>
        public async Task<ExamPaper> CopyPaperAsync(int paperId, string newTitle, int creatorId)
        {
            var originalPaper = await GetPaperWithQuestionsAsync(paperId);
            if (originalPaper == null)
                throw new ArgumentException("试卷不存在", nameof(paperId));

            var newPaper = new ExamPaper
            {
                Name = newTitle,
                Description = originalPaper.Description,
                Duration = originalPaper.Duration,
                TotalScore = originalPaper.TotalScore,
                PassScore = originalPaper.PassScore,
                CreatorId = creatorId,
                Status = "草稿",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await AddAsync(newPaper);

            // 复制题目关联
            if (originalPaper.PaperQuestions != null && originalPaper.PaperQuestions.Any())
            {
                foreach (var pq in originalPaper.PaperQuestions)
                {
                    await AddQuestionToPaperAsync(newPaper.PaperId, pq.QuestionId, pq.Score, pq.OrderIndex);
                }
            }

            return newPaper;
        }

        /// <summary>
        /// 发布/取消发布试卷
        /// </summary>
        public async Task SetPaperPublishStatusAsync(int paperId, bool isPublished)
        {
            var paper = await GetByIdAsync(paperId);
            if (paper != null)
            {
                paper.Status = isPublished ? "已发布" : "草稿";
                paper.UpdatedAt = DateTime.Now;
                await UpdateAsync(paper);
            }
        }

        /// <summary>
        /// 获取试卷统计信息
        /// </summary>
        public async Task<(int TotalPapers, int PublishedPapers, int DraftPapers)> GetPaperStatisticsAsync(int? creatorId = null)
        {
            var query = _dbSet.AsQueryable();

            if (creatorId.HasValue)
            {
                query = query.Where(p => p.CreatorId == creatorId.Value);
            }

            var totalPapers = await query.CountAsync();
            var publishedPapers = await query.CountAsync(p => p.Status == "已发布");
            var draftPapers = totalPapers - publishedPapers;

            return (totalPapers, publishedPapers, draftPapers);
        }

        /// <summary>
        /// 检查试卷是否被考试使用
        /// </summary>
        public async Task<bool> IsPaperUsedInExamsAsync(int paperId)
        {
            return await _context.ExamRecords.AnyAsync(er => er.PaperId == paperId);
        }

        /// <summary>
        /// 获取使用该试卷的考试记录
        /// </summary>
        public async Task<IEnumerable<ExamRecord>> GetExamRecordsUsingPaperAsync(int paperId)
        {
            return await _context.ExamRecords
                                .Where(er => er.PaperId == paperId)
                                .Include(er => er.User)
                                .OrderByDescending(er => er.StartTime)
                                .ToListAsync();
        }

        /// <summary>
        /// 计算试卷总分
        /// </summary>
        public async Task<decimal> CalculatePaperTotalScoreAsync(int paperId)
        {
            return await _context.PaperQuestions
                                .Where(pq => pq.PaperId == paperId)
                                .SumAsync(pq => pq.Score);
        }

        /// <summary>
        /// 获取试卷题目数量
        /// </summary>
        public async Task<int> GetPaperQuestionCountAsync(int paperId)
        {
            return await _context.PaperQuestions
                                .CountAsync(pq => pq.PaperId == paperId);
        }

        /// <summary>
        /// 清空试卷所有题目
        /// </summary>
        public async Task ClearPaperQuestionsAsync(int paperId)
        {
            var paperQuestions = await _context.PaperQuestions
                                              .Where(pq => pq.PaperId == paperId)
                                              .ToListAsync();
            if (paperQuestions.Any())
            {
                _context.PaperQuestions.RemoveRange(paperQuestions);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 根据ID获取试卷（重写以使用PaperId）
        /// </summary>
        public override async Task<ExamPaper?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.PaperId == id);
        }

        /// <summary>
        /// 根据ID删除试卷（重写以使用PaperId）
        /// </summary>
        public override async Task<bool> RemoveByIdAsync(int id)
        {
            var paper = await GetByIdAsync(id);
            if (paper == null)
                return false;

            // 先清空试卷题目
            await ClearPaperQuestionsAsync(id);
            
            await RemoveAsync(paper);
            return true;
        }
    }
}