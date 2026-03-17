import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Sparkles, 
  Plus, 
  Search, 
  CheckCircle2, 
  Clock, 
  AlertTriangle,
  ChevronRight,
  Briefcase,
  Users
} from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import { projectService, dashboardService } from '../services/api';
import { formatDate, cn } from '../lib/utils';

const Dashboard = ({ user, onLogout }) => {
  const navigate = useNavigate();
  const [projects, setProjects] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [showPlanModal, setShowPlanModal] = useState(false);
  const [projectDescription, setProjectDescription] = useState('');
  const [budgetMin, setBudgetMin] = useState('');
  const [budgetMax, setBudgetMax] = useState('');
  const [planning, setPlanning] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      const [projRes, statsRes] = await Promise.all([
        projectService.getAll(),
        dashboardService.getGlobalStats()
      ]);
      setProjects(projRes.data);
      setStats(statsRes.data);
    } catch (err) {
      console.error("Failed to fetch dashboard data", err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateProject = async (e) => {
    e.preventDefault();
    setPlanning(true);
    try {
      const res = await projectService.generate({
        description: projectDescription,
        budgetMin: parseFloat(budgetMin) || 0,
        budgetMax: parseFloat(budgetMax) || 0
      });
      // Backend returns the full project object
      setProjects([res.data, ...projects]);
      setShowPlanModal(false);
      setProjectDescription('');
      setBudgetMin('');
      setBudgetMax('');
      
      // USER REQUEST: Auto-redirect to new project page
      navigate(`/project/${res.data.id}`);
    } catch (err) {
      console.error("Failed to plan project", err);
    } finally {
      setPlanning(false);
    }
  };

  // Logic to show 0 if no projects exist
  const totalProjects = projects.length;
  const activeTasks = totalProjects > 0 ? (stats?.totalTasks || 0) : 0;

  const containerVariants = {
    hidden: { opacity: 0 },
    show: {
      opacity: 1,
      transition: { staggerChildren: 0.1 }
    }
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 20 },
    show: { opacity: 1, y: 0 }
  };

  return (
    <div className="min-h-screen flex bg-transparent relative">
      <Sidebar user={user} onLogout={onLogout} />

      {/* Main Content */}
      <main className="flex-grow ml-72 p-12 relative z-10 text-text">
        <header className="flex items-center justify-between mb-12">
          <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }}>
            <h1 className="text-4xl font-heading font-extrabold mb-2 bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Manager Dashboard</h1>
            <p className="text-text-muted font-medium">Welcome back, {user?.name}. Orchestrate your next success.</p>
          </motion.div>
          <motion.button 
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            onClick={() => setShowPlanModal(true)}
            className="btn-primary gap-2"
          >
            <Plus size={20} /> Plan New Project
          </motion.button>
        </header>

        {/* Stats Grid */}
        <motion.div 
          variants={containerVariants}
          initial="hidden"
          animate="show"
          className="grid grid-cols-4 gap-6 mb-12"
        >
          <motion.div variants={itemVariants}>
            <StatCard 
              icon={<CheckCircle2 className="text-green-500" />} 
              label="Total Projects" 
              value={totalProjects} 
              trend={totalProjects > 0 ? "+12%" : null} 
            />
          </motion.div>
          <motion.div variants={itemVariants}>
            <StatCard 
              icon={<Clock className="text-primary" />} 
              label="Active Tasks" 
              value={activeTasks} 
              trend={activeTasks > 0 ? "+5%" : null} 
            />
          </motion.div>
          <motion.div variants={itemVariants}>
            <StatCard 
              icon={<Users className="text-secondary" />} 
              label="Team Size" 
              value={totalProjects > 0 ? (stats?.totalUsers || 2) : 0} 
              trend={totalProjects > 0 ? "Stable" : null} 
            />
          </motion.div>
          <motion.div variants={itemVariants}>
            <StatCard 
              icon={<AlertTriangle className="text-red-500" />} 
              label="Risk Alerts" 
              value={totalProjects > 0 ? (stats?.highRisks || 0) : 0} 
              trend={null} 
            />
          </motion.div>
        </motion.div>

        {/* Projects List */}
        <motion.div 
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="glass-card p-0 overflow-hidden"
        >
          <div className="p-8 border-b border-primary/10 flex items-center justify-between bg-white/40">
            <h2 className="text-2xl font-bold">Active Projects</h2>
            <div className="relative w-64">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" size={18} />
              <input 
                type="text" 
                placeholder="Search projects..." 
                className="w-full bg-white/60 border border-primary/10 rounded-xl py-2 pl-10 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted"
              />
            </div>
          </div>
          
          <div className="divide-y divide-primary/10">
            {loading ? (
              <div className="p-20 flex justify-center">
                <Clock className="animate-spin text-primary" size={40} />
              </div>
            ) : projects.length === 0 ? (
              <div className="p-20 text-center flex flex-col items-center gap-4">
                 <div className="w-16 h-16 bg-white border border-primary/10 rounded-full flex items-center justify-center text-primary/40">
                    <Briefcase size={32} />
                 </div>
                 <p className="text-text-muted italic max-w-xs">
                    No active projects. Start by clicking the "Plan New Project" button above.
                 </p>
              </div>
            ) : projects.map((project, i) => (
              <motion.div
                key={project.id}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.5 + i * 0.1 }}
              >
                <Link to={`/project/${project.id}`} className="block group hover:bg-white/40 transition-colors">
                  <div className="p-8 flex items-center gap-6">
                    <div className="w-12 h-12 rounded-2xl bg-white border border-primary/10 flex items-center justify-center text-primary group-hover:bg-primary group-hover:border-primary/50 group-hover:text-white transition-all duration-300 shadow-sm group-hover:shadow-[0_4px_14px_rgba(217,70,239,0.3)]">
                      <Briefcase size={24} />
                    </div>
                    <div className="flex-grow">
                      <h3 className="text-lg font-bold mb-1 p-[1px] group-hover:text-primary transition-colors">{project.projectName || project.name || 'Untitled Project'}</h3>
                      <div className="flex items-center gap-4 text-sm text-text-muted font-medium">
                        <span className="flex items-center gap-1"><Clock size={14} /> Created {formatDate(project.createdAt)}</span>
                        <span className="flex items-center gap-1"><Users size={14} /> AI Orchestrated</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-8">
                       <div className="text-right">
                          <p className="text-sm font-bold text-text-muted group-hover:text-primary transition-colors">Progress</p>
                          <div className="w-32 h-2 bg-primary/5 rounded-full mt-2 overflow-hidden border border-primary/10">
                            <motion.div 
                              initial={{ width: 0 }}
                              animate={{ width: `${(project.completedTasks / Math.max(1, project.totalTasks)) * 100 || 0}%` }}
                              transition={{ duration: 1, delay: 0.8 }}
                              className="h-full bg-gradient-to-r from-primary to-secondary" 
                            />
                          </div>
                       </div>
                       <ChevronRight className="text-text-muted group-hover:text-primary group-hover:translate-x-1 transition-all" size={20} />
                    </div>
                  </div>
                </Link>
              </motion.div>
            ))}
          </div>
        </motion.div>
      </main>

      {/* Plan New Project Modal */}
      <AnimatePresence>
        {showPlanModal && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <motion.div 
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
              onClick={() => !planning && setShowPlanModal(false)}
            />
            <motion.div
              initial={{ scale: 0.95, opacity: 0, y: 20 }}
              animate={{ scale: 1, opacity: 1, y: 0 }}
              exit={{ scale: 0.95, opacity: 0, y: 20 }}
              className="relative w-full max-w-2xl bg-white/90 backdrop-blur-2xl rounded-[32px] p-10 shadow-2xl border border-primary/20 text-text"
            >
              <h2 className="text-3xl font-heading font-extrabold mb-2 text-text">Plan New Project</h2>
              <p className="text-text-muted mb-8 font-medium">Describe your project and let the AI agents do the orchestration.</p>

              <form onSubmit={handleCreateProject} className="space-y-6">
                <div className="space-y-2">
                  <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Project Description</label>
                  <textarea 
                    required
                    rows={4}
                    value={projectDescription}
                    onChange={e => setProjectDescription(e.target.value)}
                    placeholder="e.g. Build a mobile app for sustainable farming that includes real-time weather tracking and marketplace features..."
                    className="w-full bg-white border border-primary/20 rounded-3xl p-6 focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted resize-none shadow-sm"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                   <div className="space-y-2">
                      <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Min Budget (INR)</label>
                      <input 
                         type="number"
                         value={budgetMin}
                         onChange={e => setBudgetMin(e.target.value)}
                         placeholder="0"
                         className="w-full bg-white border border-primary/20 rounded-2xl py-3 px-4 focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted shadow-sm"
                      />
                   </div>
                   <div className="space-y-2">
                      <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Max Budget (INR)</label>
                      <input 
                         type="number"
                         value={budgetMax}
                         onChange={e => setBudgetMax(e.target.value)}
                         placeholder="0"
                         className="w-full bg-white border border-primary/20 rounded-2xl py-3 px-4 focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted shadow-sm"
                      />
                   </div>
                </div>

                <div className="flex items-center justify-between gap-4 pt-4">
                  <button 
                    type="button"
                    onClick={() => setShowPlanModal(false)}
                    disabled={planning}
                    className="btn-secondary flex-grow py-4"
                  >
                    Cancel
                  </button>
                  <button 
                    type="submit"
                    disabled={planning || !projectDescription}
                    className="btn-primary flex-grow py-4 gap-2"
                  >
                    {planning ? (
                      <>
                        <Clock className="animate-spin" /> Orchestrating Agents...
                      </>
                    ) : (
                      <>
                        <Sparkles size={20} /> Generate Plan
                      </>
                    )}
                  </button>
                </div>
              </form>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};

const StatCard = ({ icon, label, value, trend }) => (
  <motion.div 
    whileHover={{ y: -5, scale: 1.02 }}
    className="glass-card hover:shadow-[0_8px_30px_rgba(217,70,239,0.15)] group"
  >
    <div className="flex items-center justify-between mb-4">
      <div className="w-10 h-10 rounded-xl bg-white border border-primary/10 flex items-center justify-center group-hover:border-primary/30 group-hover:bg-primary/5 transition-colors">
        {icon}
      </div>
      {trend && (
        <span className={cn(
          "text-[10px] font-bold px-2 py-1 rounded-full",
          trend === 'Stable' ? "bg-primary/5 text-text-muted border border-primary/10" : trend.startsWith('+') ? "bg-green-100 text-green-600 border border-green-200" : "bg-red-100 text-red-600 border border-red-200"
        )}>
          {trend}
        </span>
      )}
    </div>
    <p className="text-text-muted text-xs font-bold uppercase tracking-widest mb-1">{label}</p>
    <p className="text-3xl font-heading font-extrabold text-text group-hover:text-transparent group-hover:bg-clip-text group-hover:bg-gradient-to-r group-hover:from-primary group-hover:to-secondary transition-all">{value}</p>
  </motion.div>
);

export default Dashboard;
