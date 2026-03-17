import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Sparkles, 
  ChevronLeft, 
  Layers, 
  Target, 
  AlertTriangle, 
  Users, 
  CheckCircle2,
  Clock,
  ArrowRight,
  UserPlus,
  X,
  Check
} from 'lucide-react';
import { projectService, authService } from '../services/api';
import { formatDate, cn } from '../lib/utils';

const ProjectView = ({ user, onLogout }) => {
  const { id } = useParams();
  const [project, setProject] = useState(null);
  const [loading, setLoading] = useState(true);
  const [team, setTeam] = useState([]);
  const [showAssignModal, setShowAssignModal] = useState(null); // stores task object

  useEffect(() => {
    fetchProject();
    fetchTeam();
  }, [id]);

  const fetchProject = async () => {
    try {
      const res = await projectService.getById(id);
      setProject(res.data);
    } catch (err) {
      console.error("Failed to fetch project", err);
    } finally {
      setLoading(false);
    }
  };

  const fetchTeam = async () => {
     try {
        const res = await authService.getUsers();
        setTeam(res.data || []);
     } catch (err) {
        console.error("Failed to fetch team", err);
     }
  };

  const handleAssign = async (userId) => {
     try {
        await projectService.assignTask(id, showAssignModal.id, userId);
        setShowAssignModal(null);
        fetchProject(); // Refresh
     } catch (err) {
        console.error("Assignment failed", err);
     }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <Clock className="animate-spin text-primary" size={40} />
      </div>
    );
  }

  if (!project) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center bg-background p-8">
        <h2 className="text-2xl font-bold mb-4">Project not found</h2>
        <Link to="/" className="btn-primary">Go Back</Link>
      </div>
    );
  }

  // Helper for naming cleanup
  const projectName = project.projectName || project.name || "Untitled Project";
  const projectDescription = project.description || "No description provided.";

  return (
    <div className="min-h-screen bg-background pb-20">
      {/* Header Bar */}
      <div className="bg-white border-b border-border px-8 py-4 flex items-center justify-between sticky top-0 z-30">
        <div className="flex items-center gap-6">
          <Link to="/" className="p-2 hover:bg-slate-50 rounded-xl transition-all">
            <ChevronLeft size={20} className="text-text-muted" />
          </Link>
          <div className="h-6 w-px bg-border" />
          <h1 className="text-xl font-heading font-extrabold text-text">{projectName}</h1>
        </div>
        
        <div className="flex items-center gap-4">
           <div className="px-4 py-1.5 rounded-full bg-primary/10 text-primary text-[10px] font-bold uppercase tracking-widest border border-primary/20">
              ID: {id?.substring(0, 8)}...
           </div>
        </div>
      </div>

      <main className="max-w-7xl mx-auto px-8 py-12">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Main Info */}
          <div className="lg:col-span-2 space-y-8">
             <div className="glass-card p-10">
                <div className="flex items-center gap-3 mb-6">
                   <Target className="text-primary" size={28} />
                   <h2 className="text-2xl font-heading font-extrabold">Executive Summary</h2>
                </div>
                <p className="text-lg text-text-muted leading-relaxed font-medium">
                   {projectDescription}
                </p>
             </div>

             {/* Phases & Tasks */}
             <div className="space-y-6">
                <div className="flex items-center justify-between">
                   <h3 className="text-2xl font-heading font-extrabold">Project Phases</h3>
                   <span className="text-sm font-bold text-text-muted">{project.phases?.length || 0} Phases Planned</span>
                </div>
                
                {project.phases?.map((phase, pIdx) => (
                   <div key={phase.id || pIdx} className="bg-white rounded-[32px] border border-border shadow-sm overflow-hidden">
                      <div className="bg-slate-50 px-8 py-4 border-b border-border flex items-center justify-between">
                         <div className="flex items-center gap-3">
                            <Layers className="text-primary/60" size={18} />
                            <span className="font-bold text-text uppercase tracking-widest text-xs">Phase {pIdx + 1}: {phase.name || 'General'}</span>
                         </div>
                         <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-text-muted">{phase.tasks?.length || 0} tasks</span>
                         </div>
                      </div>
                      <div className="divide-y divide-border">
                         {phase.tasks?.map((task, tIdx) => {
                            const assignee = task.assignedTo || task.assigneeName;
                            return (
                            <div key={task.id || tIdx} className="p-6 flex items-center justify-between group hover:bg-slate-50 transition-colors">
                               <div className="flex items-center gap-4">
                                  <div className={cn(
                                     "w-3 h-3 rounded-full border-2",
                                     task.status === 'Completed' ? "bg-green-500 border-green-500" : "bg-white border-slate-300"
                                  )} />
                                  <div className="flex flex-col">
                                     <span className={cn("font-bold text-sm", (task.status === 'Completed' || task.status === 2) && "line-through text-text-muted")}>
                                        {task.name || 'Untitled Task'}
                                     </span>
                                     <span className="text-[10px] text-text-muted font-bold uppercase tracking-tighter">
                                        ID: {String(task.id || '').split('-')[0] || 'N/A'}
                                     </span>
                                  </div>
                               </div>
                               <div className="flex items-center gap-6">
                                  {assignee ? (
                                     <div 
                                        onClick={() => setShowAssignModal(task)}
                                        className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-primary/10 border border-primary/20 cursor-pointer hover:bg-primary/20 transition-all"
                                     >
                                        <div className="w-5 h-5 rounded-full bg-primary flex items-center justify-center text-[10px] font-bold text-white shadow-sm">
                                           {String(assignee).charAt(0)}
                                        </div>
                                        <span className="text-xs font-bold text-primary">{assignee}</span>
                                     </div>
                                  ) : (
                                     <button 
                                        onClick={() => setShowAssignModal(task)}
                                        className="text-xs font-bold text-primary hover:bg-primary/10 px-4 py-2 rounded-xl border border-primary/20 flex items-center gap-2 transition-all active:scale-95"
                                     >
                                        <UserPlus size={14} /> Assign
                                     </button>
                                  )}
                               </div>
                            </div>
                         )})}
                      </div>
                   </div>
                ))}
             </div>
          </div>

          {/* Sidebar Info */}
          <div className="space-y-8">
             {/* Financials */}
             <div className="glass-card">
                <h3 className="text-lg font-bold mb-6">Financial Overview</h3>
                <div className="space-y-4">
                   <div className="flex justify-between items-center p-4 rounded-2xl bg-primary/5 border border-primary/10">
                      <span className="text-sm font-bold text-text-muted">Min Budget</span>
                      <span className="text-sm font-extrabold text-primary">₹{(project.estimatedCostMin || 0).toLocaleString()}</span>
                   </div>
                   <div className="flex justify-between items-center p-4 rounded-2xl bg-primary/5 border border-primary/10">
                      <span className="text-sm font-bold text-text-muted">Max Budget</span>
                      <span className="text-sm font-extrabold text-primary">₹{(project.estimatedCostMax || 0).toLocaleString()}</span>
                   </div>
                </div>
             </div>

             {/* Risks */}
             <div className="glass-card bg-orange-50/30 border-orange-100">
                <div className="flex items-center gap-2 mb-6 text-orange-600">
                   <AlertTriangle size={20} />
                   <h3 className="font-heading font-extrabold uppercase tracking-widest text-xs">AI Risk Assessment</h3>
                </div>
                <div className="space-y-4">
                   {project.risks?.map((risk, rIdx) => (
                      <div key={risk.id || rIdx} className="p-4 bg-white rounded-2xl border border-orange-100 shadow-sm">
                         <div className="flex items-center justify-between mb-2">
                            <span className={cn(
                               "text-[10px] font-extrabold px-2 py-0.5 rounded-full",
                               (risk.severity === 'High' || risk.severity === 2) ? "bg-red-100 text-red-600" : "bg-orange-100 text-orange-600"
                            )}>
                               {String(risk.severity || 'Medium').toUpperCase()}
                            </span>
                         </div>
                         <p className="text-sm font-bold text-text leading-snug">{risk.description || risk.title}</p>
                         <p className="text-[10px] font-bold text-text-muted mt-2">MITIGATION: {risk.mitigationNote || risk.mitigation || 'N/A'}</p>
                      </div>
                   ))}
                </div>
             </div>
          </div>
        </div>
      </main>

      {/* Assignment Modal */}
      <AnimatePresence>
         {showAssignModal && (
            <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
               <motion.div 
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  onClick={() => setShowAssignModal(null)}
                  className="absolute inset-0 bg-text/40 backdrop-blur-md"
               />
               <motion.div
                  initial={{ scale: 0.9, opacity: 0, y: 20 }}
                  animate={{ scale: 1, opacity: 1, y: 0 }}
                  exit={{ scale: 0.9, opacity: 0, y: 20 }}
                  className="relative w-full max-w-md bg-white rounded-[40px] p-10 shadow-3xl overflow-hidden border border-white/50"
               >
                  <div className="absolute top-0 left-0 w-full h-2 bg-primary" />
                  
                  <button 
                     onClick={() => setShowAssignModal(null)}
                     className="absolute right-8 top-8 p-2 hover:bg-slate-100 rounded-full text-slate-400 hover:text-slate-600 transition-all"
                  >
                     <X size={20} />
                  </button>

                  <div className="mb-8">
                     <span className="text-[10px] font-bold text-primary uppercase tracking-widest mb-2 block">Assign Task</span>
                     <h3 className="text-2xl font-heading font-extrabold text-text line-clamp-2">
                        {showAssignModal.name || 'Untitled Task'}
                     </h3>
                  </div>

                  <div className="space-y-3 max-h-[400px] overflow-y-auto pr-2 custom-scrollbar">
                     {team.map((member, mIdx) => (
                        <button
                           key={member.id || mIdx}
                           onClick={() => handleAssign(member.id)}
                           className={cn(
                              "w-full p-4 rounded-3xl border flex items-center justify-between transition-all group",
                              showAssignModal.assignedUserId === member.id 
                                 ? "bg-primary border-primary text-white shadow-lg shadow-primary/20" 
                                 : "bg-slate-50 border-slate-100 hover:border-primary/30 hover:bg-white text-text"
                           )}
                        >
                           <div className="flex items-center gap-4">
                              <div className={cn(
                                 "w-10 h-10 rounded-2xl flex items-center justify-center font-bold text-sm",
                                 showAssignModal.assignedUserId === member.id ? "bg-white/20" : "bg-primary/10 text-primary group-hover:bg-primary group-hover:text-white"
                              )}>
                                 {String(member.name || 'U').charAt(0)}
                              </div>
                              <div className="text-left">
                                 <p className="font-bold text-sm">{member.name || 'Unknown'}</p>
                                 <p className={cn("text-[10px] font-bold uppercase opacity-60", showAssignModal.assignedUserId === member.id ? "text-white" : "text-text-muted")}>
                                    {member.role || 'Member'}
                                 </p>
                              </div>
                           </div>
                           {showAssignModal.assignedUserId === member.id && (
                              <div className="bg-white/20 p-1 rounded-full"><Check size={16} /></div>
                           )}
                        </button>
                     ))}
                  </div>
               </motion.div>
            </div>
         )}
      </AnimatePresence>
    </div>
  );
};

export default ProjectView;
